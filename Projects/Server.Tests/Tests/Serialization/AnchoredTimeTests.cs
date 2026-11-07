using System;
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

    private static readonly DateTime SaveStart = new(2026, 8, 22, 12, 0, 0, DateTimeKind.Utc);

    private static IGenericReader ReadSaved(BufferWriter writer, TimeSpan downtime) =>
        new BufferReader(writer.Buffer) { AnchoredTimeShift = downtime, AnchoredTimeOrigin = SaveStart };

    [Fact]
    public void AnchoredTime_DeadlineRunningAtSaveStart_KeepsItsRemainingTime()
    {
        var writer = new BufferWriter(new byte[64], true);
        var deadline = SaveStart + TimeSpan.FromMinutes(20);
        var downtime = TimeSpan.FromHours(3);

        writer.WriteAnchoredTime(deadline);

        Assert.Equal(deadline + downtime, ReadSaved(writer, downtime).ReadAnchoredTime());
    }

    /// <summary>
    /// A deadline that had passed before the save must stay passed. Shifting it brought an expiry
    /// back into the future whenever the downtime was longer than the time since it expired.
    /// </summary>
    [Fact]
    public void AnchoredTime_DeadlinePassedBeforeSaveStart_StaysPassed()
    {
        var writer = new BufferWriter(new byte[64], true);
        var expired = SaveStart - TimeSpan.FromMinutes(5);

        writer.WriteAnchoredTime(expired);

        Assert.Equal(expired, ReadSaved(writer, TimeSpan.FromMinutes(30)).ReadAnchoredTime());
    }

    /// <summary>
    /// A stamp of something that already happened (a last-use time) is never shifted, so its
    /// bytes survive a restart and downtime counts toward its window.
    /// </summary>
    [Fact]
    public void AnchoredTime_PastStamp_IsNotShifted()
    {
        var writer = new BufferWriter(new byte[64], true);
        var lastUse = SaveStart - TimeSpan.FromDays(2);

        writer.WriteAnchoredTime(lastUse);

        Assert.Equal(lastUse, ReadSaved(writer, TimeSpan.FromHours(1)).ReadAnchoredTime());
    }

    [Fact]
    public void AnchoredTime_ValueAtSaveStart_IsNotShifted()
    {
        var writer = new BufferWriter(new byte[64], true);

        writer.WriteAnchoredTime(SaveStart);

        Assert.Equal(SaveStart, ReadSaved(writer, TimeSpan.FromHours(1)).ReadAnchoredTime());
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
        // The scope also restores World.SaveStartTime, which this test backdates.
        using var pipeline = new PersistencePipelineScope(
            2,
            installWorkers: true,
            visibleAssembly: typeof(AnchoredEntity).Assembly,
            directoryPrefix: "muo-anchored"
        );
        var source = pipeline.Source;
        var dir = pipeline.Directory;

        var persistence = pipeline.Track(new AnchoredPersistence(2100));

        var lastRested = Core.Now - TimeSpan.FromMinutes(10);
        var serial = (Serial)1u;
        persistence.EntitiesBySerial[serial] = new AnchoredEntity(serial) { LastRested = lastRested };
        persistence.RegisterType(typeof(AnchoredEntity));

        // Pretend the save started two hours ago, as if the server had been down since.
        var downtime = TimeSpan.FromHours(2);
        World.SaveStartTime = Core.Now - downtime;

        pipeline.WakeWorkers();

        source.SetOwner(persistence);
        Assert.True(persistence.TrySnapshotEntries(out var slotCount));
        source.PushSlotRanges(persistence, slotCount);

        pipeline.FlushAndSleepWorkers();

        persistence.WriteSnapshot(dir);
        persistence.PostWorldSave();

        var loaded = pipeline.Track(new AnchoredPersistence(2101));
        loaded.DeserializeIndexes(dir, null);
        loaded.Deserialize(dir, null);

        var entity = loaded.EntitiesBySerial[serial];
        var expected = lastRested + downtime;

        Assert.True(
            (entity.LastRested - expected).Duration() <= TimeSpan.FromSeconds(30),
            $"Anchored timestamp must re-base by the downtime; expected ~{expected}, got {entity.LastRested}."
        );
    }
}
