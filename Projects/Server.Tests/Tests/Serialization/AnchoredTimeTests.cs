using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Server.Tests;

public class AnchoredTimeTests
{
    private static (BufferWriter Writer, Func<TimeSpan, IGenericReader> Read) CreateRoundTrip()
    {
        var writer = new BufferWriter(new byte[64], true);
        return (writer, shift => new BufferReader(writer.Buffer) { AnchoredTimeShift = shift });
    }

    [Fact]
    public void AnchoredTime_RoundTripsExactly_WithZeroShift()
    {
        var (writer, read) = CreateRoundTrip();
        var value = new DateTime(2026, 8, 22, 12, 30, 0, DateTimeKind.Utc);

        writer.WriteAnchoredTime(value);

        Assert.Equal(value, read(TimeSpan.Zero).ReadAnchoredTime());
    }

    [Fact]
    public void AnchoredTime_AppliesShiftOnRead()
    {
        var (writer, read) = CreateRoundTrip();
        var value = new DateTime(2026, 8, 22, 12, 30, 0, DateTimeKind.Utc);
        var shift = TimeSpan.FromHours(3);

        writer.WriteAnchoredTime(value);

        Assert.Equal(value + shift, read(shift).ReadAnchoredTime());
    }

    [Fact]
    public void AnchoredTime_SentinelsPassThroughUnshifted()
    {
        var (writer, read) = CreateRoundTrip();

        writer.WriteAnchoredTime(DateTime.MinValue);
        writer.WriteAnchoredTime(DateTime.MaxValue);

        var reader = read(TimeSpan.FromDays(2));
        Assert.Equal(DateTime.MinValue, reader.ReadAnchoredTime());
        Assert.Equal(DateTime.MaxValue, reader.ReadAnchoredTime());
    }

    [Fact]
    public void AnchoredTime_SaturatesInsteadOfOverflowing()
    {
        var (writer, read) = CreateRoundTrip();

        writer.WriteAnchoredTime(DateTime.MaxValue - TimeSpan.FromMinutes(1));

        Assert.Equal(DateTime.MaxValue, read(TimeSpan.FromDays(1)).ReadAnchoredTime());
    }

    [Fact]
    public void AnchoredTime_NormalizesLocalKindOnWrite()
    {
        var (writer, read) = CreateRoundTrip();
        var local = new DateTime(2026, 8, 22, 12, 30, 0, DateTimeKind.Local);

        writer.WriteAnchoredTime(local);

        Assert.Equal(local.ToUniversalTime(), read(TimeSpan.Zero).ReadAnchoredTime());
    }
}

internal class AnchoredEntity : ISerializable
{
    public AnchoredEntity(Serial serial) => Serial = serial;

    public Serial Serial { get; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public bool Deleted => false;

    public DateTime LastRested { get; set; }

    public void Delete()
    {
    }

    public void Serialize(IGenericWriter writer) => writer.WriteAnchoredTime(LastRested);

    public void Deserialize(IGenericReader reader) => LastRested = reader.ReadAnchoredTime();
}

[Collection("Sequential Server Tests")]
public class AnchoredTimePersistenceTests
{
    private class AnchoredPersistence : GenericEntityPersistence<AnchoredEntity>
    {
        public AnchoredPersistence(int priority) : base("AnchoredTrip", priority, 1, 0x7FFFFFFF)
        {
        }
    }

    /// <summary>
    /// The idx v5 header carries the save-start anchor; loading re-bases anchored timestamps
    /// by the elapsed time since the save started, so downtime does not age them.
    /// </summary>
    [Fact]
    public void SaveStartAnchor_RebasesAnchoredTimestampsAtLoad()
    {
        var previousAssemblies = AssemblyHandler.Assemblies;
        AssemblyHandler.Assemblies = [.. previousAssemblies ?? [], typeof(AnchoredEntity).Assembly];

        var source = new SerializationChunkSource();
        var workers = new SerializationThreadWorker[2];
        for (var i = 0; i < workers.Length; i++)
        {
            workers[i] = new SerializationThreadWorker(i, source);
            workers[i].AllocateHeap();
        }

        var previousWorkers = World._threadWorkers;
        World._threadWorkers = workers;

        var previousSaveStart = World.SaveStartTime;

        var persistence = new AnchoredPersistence(2100);
        AnchoredPersistence loaded = null;

        var dir = Path.Combine(Path.GetTempPath(), $"muo-anchored-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        try
        {
            var lastRested = Core.Now - TimeSpan.FromMinutes(10);
            var serial = (Serial)1u;
            persistence.EntitiesBySerial[serial] = new AnchoredEntity(serial) { LastRested = lastRested };
            persistence.RegisterType(typeof(AnchoredEntity));

            // Pretend the save started two hours ago, as if the server had been down since.
            var downtime = TimeSpan.FromHours(2);
            World.SaveStartTime = Core.Now - downtime;

            foreach (var worker in workers)
            {
                worker.Wake();
            }

            source.SetOwner(persistence);
            Assert.True(persistence.TrySnapshotEntries(out var slotCount));
            source.PushSlotRanges(persistence, slotCount);

            source.Flush();
            foreach (var worker in workers)
            {
                worker.Sleep();
            }

            persistence.WriteSnapshot(dir);
            persistence.PostWorldSave();

            loaded = new AnchoredPersistence(2101);
            loaded.DeserializeIndexes(dir, null);
            loaded.Deserialize(dir, null);

            var entity = loaded.EntitiesBySerial[serial];
            var expected = lastRested + downtime;

            Assert.True(
                (entity.LastRested - expected).Duration() <= TimeSpan.FromSeconds(30),
                $"Anchored timestamp must re-base by the downtime; expected ~{expected}, got {entity.LastRested}."
            );
        }
        finally
        {
            World.SaveStartTime = previousSaveStart;
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
