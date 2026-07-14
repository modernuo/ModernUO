using System;
using System.Collections.Generic;
using System.IO;
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

    public void Serialize(IGenericWriter writer)
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
        // The loader resolves types by hash through AssemblyHandler; make this test
        // assembly visible for the duration of the test.
        var previousAssemblies = AssemblyHandler.Assemblies;
        AssemblyHandler.Assemblies = [.. previousAssemblies ?? [], typeof(RoundTripEntity).Assembly];

        var source = new SerializationChunkSource();
        var workers = new SerializationThreadWorker[3];
        for (var i = 0; i < workers.Length; i++)
        {
            workers[i] = new SerializationThreadWorker(i, source);
            workers[i].AllocateHeap();
        }

        var previousWorkers = World._threadWorkers;
        World._threadWorkers = workers;

        var persistence = new RoundTripPersistence(2000);
        RoundTripPersistence loaded = null;

        var dir = Path.Combine(Path.GetTempPath(), $"muo-roundtrip-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        try
        {
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

            // Freeze: what Persistence.SerializeAll + GenericEntityPersistence.Serialize do,
            // against this test's chunk source instead of the world's.
            foreach (var worker in workers)
            {
                worker.Wake();
            }

            source.SetOwner(persistence);
            source.PushSingle(persistence);
            Assert.True(persistence.TrySnapshotEntries(out var slotCount));
            source.PushSlotRanges(persistence, slotCount);

            // Mirrors World.PauseSerializationThreads
            source.Flush();
            foreach (var worker in workers)
            {
                worker.Sleep();
            }

            // Background write phase: snapshot from the segment logs, then the types db.
            var typeSet = new HashSet<Type>();
            persistence.WriteSnapshot(dir, typeSet);

            var typesDb = new Dictionary<ulong, string>();
            foreach (var type in typeSet)
            {
                typesDb[AssemblyHandler.GetTypeHash(type)] = type.FullName;
            }

            persistence.PostWorldSave(); // releases the entries snapshot

            // Load into a fresh persistence, like a server boot would.
            loaded = new RoundTripPersistence(2001);
            loaded.DeserializeIndexes(dir, typesDb);
            loaded.Deserialize(dir, typesDb);

            Assert.True(loaded.SelfPayloadDeserialized);
            Assert.Equal(persistence.EntitiesBySerial.Count, loaded.EntitiesBySerial.Count);

            foreach (var (serial, original) in persistence.EntitiesBySerial)
            {
                var entity = loaded.EntitiesBySerial[serial];
                Assert.Equal(original.Value, entity.Value);
                Assert.Equal(original.Name, entity.Name);
                Assert.Equal(original.Created.Ticks, entity.Created.Ticks);
            }
        }
        finally
        {
            persistence.Unregister();
            loaded?.Unregister();

            foreach (var worker in workers)
            {
                worker.Exit();
            }

            World._threadWorkers = previousWorkers;
            AssemblyHandler.Assemblies = previousAssemblies;
            Directory.Delete(dir, true);
        }
    }
}
