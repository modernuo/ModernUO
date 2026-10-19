using System;
using Xunit;

namespace Server.Tests;

internal class RoundTripEntity : ISerializable
{
    public RoundTripEntity(Serial serial) => Serial = serial;

    public Serial Serial { get; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public bool Deleted => false;

    public int Value { get; set; }
    public string Name { get; set; }

    public void Delete()
    {
    }

    public virtual void Serialize(IGenericWriter writer)
    {
        writer.Write(Value);
        writer.Write(Name);
    }

    public void Deserialize(IGenericReader reader)
    {
        Value = reader.ReadInt();
        Name = reader.ReadString();
    }
}

internal class ThrowingRoundTripEntity : RoundTripEntity
{
    public ThrowingRoundTripEntity(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer) => throw new InvalidOperationException("broken serializer");
}

[Collection("Sequential Server Tests")]
public class GenericEntityPersistenceRoundTripTests
{
    private const uint SelfPayloadMarker = 0xDEADBEEF;

    private class RoundTripPersistence : GenericEntityPersistence<RoundTripEntity>
    {
        public bool SelfPayloadDeserialized { get; private set; }

        public RoundTripPersistence(int priority) : base("RoundTrip", priority, 1, 0x7FFFFFFF)
        {
        }

        public override void Serialize(IGenericWriter writer) => writer.Write(SelfPayloadMarker);

        public override void Deserialize(IGenericReader reader)
        {
            Assert.Equal(SelfPayloadMarker, reader.ReadUInt());
            SelfPayloadDeserialized = true;
        }
    }

    /// <summary>
    /// Drives the real pipeline end to end: entities serialize on workers via slot-range
    /// chunks (plus the persistence self-payload as a single), WriteSnapshot assembles the
    /// idx/bin from the per-worker segment logs, and a fresh persistence loads it all back.
    /// </summary>
    [Fact]
    public void SnapshotRoundTripsThroughWorkersAndSegmentLogs()
    {
        // The loader resolves types by hash through AssemblyHandler, so this test assembly must be visible.
        using var pipeline = new PersistencePipelineScope(
            3,
            installWorkers: true,
            visibleAssembly: typeof(RoundTripEntity).Assembly,
            directoryPrefix: "muo-roundtrip"
        );
        var source = pipeline.Source;
        var dir = pipeline.Directory;

        var persistence = pipeline.Track(new RoundTripPersistence(2000));

        const int entityCount = 25_000;
        var rng = new System.Random(0x5EED);

        for (var i = 1; i <= entityCount; i++)
        {
            var serial = (Serial)(uint)i;
            persistence.EntitiesBySerial[serial] = new RoundTripEntity(serial)
            {
                Value = rng.Next(),
                Name = i % 5 == 0 ? null : $"entity-{i}"
            };
        }

        persistence.RegisterType(typeof(RoundTripEntity));

        // Freeze: what Persistence.SerializeAll + GenericEntityPersistence.Serialize do,
        // against this test's chunk source instead of the world's.
        pipeline.WakeWorkers();

        source.SetOwner(persistence);
        source.PushSingle(persistence);
        Assert.True(persistence.TrySnapshotEntries(out var slotCount));
        source.PushSlotRanges(persistence, slotCount);

        pipeline.FlushAndSleepWorkers();

        // Background write phase: snapshot from the segment logs. v4 embeds the type
        // table in the idx, so no SerializedTypes.db is produced or needed.
        persistence.WriteSnapshot(dir);

        persistence.PostWorldSave(); // releases the entries snapshot

        // Load into a fresh persistence, like a server boot would.
        var loaded = pipeline.Track(new RoundTripPersistence(2001));
        loaded.DeserializeIndexes(dir, null);
        loaded.Deserialize(dir, null);

        Assert.True(loaded.SelfPayloadDeserialized);
        Assert.Equal(persistence.EntitiesBySerial.Count, loaded.EntitiesBySerial.Count);

        foreach (var (serial, original) in persistence.EntitiesBySerial)
        {
            var entity = loaded.EntitiesBySerial[serial];
            Assert.Equal(original.Value, entity.Value);
            Assert.Equal(original.Name, entity.Name);
            Assert.Equal(original.Created.Ticks, entity.Created.Ticks);
        }

        // Loading must hydrate the type table so the next save can write indexes.
        Assert.True(loaded.TryGetTypeIndex(typeof(RoundTripEntity), out _));
    }

    /// <summary>
    /// A serializer throwing on a worker used to be an unhandled exception on that thread.
    /// The worker records it, finishes the drain so the handshake completes, and the loop
    /// fails the save.
    /// </summary>
    [Fact]
    public void SerializerException_IsRecordedOnTheWorker_AndTheDrainCompletes()
    {
        using var pipeline = new PersistencePipelineScope(2);
        var source = pipeline.Source;
        var workers = pipeline.Workers;

        var persistence = pipeline.Track(new RoundTripPersistence(2002));

        for (var i = 1; i <= 100; i++)
        {
            var serial = (Serial)(uint)i;
            persistence.EntitiesBySerial[serial] = i == 50
                ? new ThrowingRoundTripEntity(serial)
                : new RoundTripEntity(serial) { Value = i, Name = $"entity-{i}" };
        }

        pipeline.WakeWorkers();

        source.SetOwner(persistence);
        Assert.True(persistence.TrySnapshotEntries(out var slotCount));
        source.PushSlotRanges(persistence, slotCount);

        pipeline.FlushAndSleepWorkers();

        Exception error = null;
        foreach (var worker in workers)
        {
            error ??= worker.Error;
        }

        Assert.IsType<InvalidOperationException>(error);
        persistence.PostWorldSave();

        // The next drain starts clean.
        pipeline.WakeWorkers();

        foreach (var worker in workers)
        {
            worker.Sleep();
            Assert.Null(worker.Error);
        }
    }

    /// <summary>
    /// A segment that cannot be written used to be logged and dropped, publishing a save
    /// without those entities (the loader then deletes them). It now fails the save.
    /// </summary>
    [Fact]
    public void WriteSnapshot_FailsTheSave_InsteadOfDroppingASegment()
    {
        using var pipeline = new PersistencePipelineScope(2, installWorkers: true, directoryPrefix: "muo-segmentfail");
        var source = pipeline.Source;

        var persistence = pipeline.Track(new RoundTripPersistence(2003));

        for (var i = 1; i <= 100; i++)
        {
            var serial = (Serial)(uint)i;
            persistence.EntitiesBySerial[serial] = new RoundTripEntity(serial) { Value = i, Name = $"entity-{i}" };
        }

        // Deliberately not registered: the writer cannot index the type.

        pipeline.WakeWorkers();

        source.SetOwner(persistence);
        Assert.True(persistence.TrySnapshotEntries(out var slotCount));
        source.PushSlotRanges(persistence, slotCount);

        pipeline.FlushAndSleepWorkers();

        Assert.Throws<InvalidOperationException>(() => persistence.WriteSnapshot(pipeline.Directory));
        persistence.PostWorldSave();
    }
}
