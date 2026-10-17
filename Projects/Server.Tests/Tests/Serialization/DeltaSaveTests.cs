using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Server.Tests;

[AuditedDirtyTracking("delta save tests")]
public class DeltaEntity : ISerializable
{
    public DeltaEntity(Serial serial) => Serial = serial;

    public Serial Serial { get; }
    public DateTime Created { get; set; } = Core.Now;
    public bool Deleted { get; private set; }
    public bool SaveDirty { get; set; }
    public long SavePlacement { get; set; }

    public int Value { get; set; }
    public string Name { get; set; }

    public int SerializeCalls { get; set; }

    public virtual bool SkipSerialization => false;

    public void Delete() => Deleted = true;

    public virtual void Serialize(IGenericWriter writer)
    {
        SerializeCalls++;
        writer.Write(Value);
        writer.Write(Name);
    }

    public void Deserialize(IGenericReader reader)
    {
        Value = reader.ReadInt();
        Name = reader.ReadString();
    }
}

// No audit attribute on this class: the chain is not eligible, so it always serializes.
public class UntrustedDeltaEntity : DeltaEntity
{
    public UntrustedDeltaEntity(Serial serial) : base(serial)
    {
    }
}

[AuditedDirtyTracking("delta save tests")]
public class EmptyDeltaEntity : DeltaEntity
{
    public EmptyDeltaEntity(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
    }
}

[AuditedDirtyTracking("delta save tests")]
public class ThrowingDeltaEntity : DeltaEntity
{
    public ThrowingDeltaEntity(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer) => throw new InvalidOperationException("broken serializer");
}

[AuditedDirtyTracking("delta save tests")]
public class SkippedDeltaEntity : DeltaEntity
{
    public SkippedDeltaEntity(Serial serial) : base(serial)
    {
    }

    public override bool SkipSerialization => true;
}

[Collection("Sequential Server Tests")]
public class DeltaSaveTests : IDisposable
{
    private class DeltaPersistence : GenericEntityPersistence<DeltaEntity>
    {
        public DeltaPersistence(int priority) : base("Delta", priority, 1, 0x7FFFFFFF)
        {
        }

        public string SourcePath { get; set; }

        protected override string CommittedBinPath => SourcePath;

        public bool CopySourceValid => ValidateCopySource();

        public void FailSave() => OnSaveFailed();
    }

    private static readonly SavePlan FullPlan = new(true, false, 0, 1, "test full");
    private static readonly SavePlan FullVerifyPlan = new(true, true, 0, 2, "test verify");
    private static readonly SavePlan DeltaPlan = new(false, true, 0, 3, "test delta");
    private static readonly SavePlan DeltaSampleAllPlan = new(false, true, uint.MaxValue, 4, "test delta sampled");

    private readonly System.Reflection.Assembly[] _previousAssemblies;
    private readonly SerializationThreadWorker[] _previousWorkers;
    private readonly SerializationChunkSource _source = new();
    private readonly SerializationThreadWorker[] _workers;
    private readonly DeltaPersistence _persistence;
    private readonly string _root;
    private readonly List<string> _saveDirs = [];
    private int _priority = 3000;

    public DeltaSaveTests()
    {
        _previousAssemblies = AssemblyHandler.Assemblies;
        AssemblyHandler.Assemblies = [.. _previousAssemblies ?? [], typeof(DeltaEntity).Assembly];

        _workers = new SerializationThreadWorker[3];
        for (var i = 0; i < _workers.Length; i++)
        {
            _workers[i] = new SerializationThreadWorker(i, _source);
            _workers[i].AllocateHeap();
        }

        _previousWorkers = World._threadWorkers;
        World._threadWorkers = _workers;

        DeltaSaves.ResetForTest();
        _persistence = new DeltaPersistence(_priority++);

        _root = Path.Combine(Path.GetTempPath(), $"muo-delta-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        _persistence.Unregister();

        foreach (var worker in _workers)
        {
            worker.Exit();
        }

        World._threadWorkers = _previousWorkers;
        AssemblyHandler.Assemblies = _previousAssemblies;
        DeltaSaves.ResetForTest();

        try
        {
            Directory.Delete(_root, true);
        }
        catch
        {
            // best effort
        }
    }

    // ---- harness ---------------------------------------------------------------------------

    private string NewSaveDir()
    {
        var dir = Path.Combine(_root, $"save-{_saveDirs.Count}");
        Directory.CreateDirectory(dir);
        _saveDirs.Add(dir);
        return dir;
    }

    /// <summary>Freeze + write, like World.Snapshot/WriteFiles against this test's persistence.</summary>
    private string Save(SavePlan plan, DeltaPersistence persistence = null)
    {
        persistence ??= _persistence;
        DeltaSaves.SetPlanForTest(plan);

        foreach (var worker in _workers)
        {
            worker.Wake();
        }

        // What GenericEntityPersistence.Serialize does, against this test's chunk source.
        persistence.RotateUnreusableSerials();
        _source.SetOwner(persistence);
        _source.PushSingle(persistence);
        Assert.True(persistence.TrySnapshotEntries(out var slotCount));
        _source.PushSlotRanges(persistence, slotCount);

        _source.Flush();
        foreach (var worker in _workers)
        {
            worker.Sleep();
        }

        // Each save gets its own anchor, as the world stamps one at every freeze.
        World.SaveStartTime = World.SaveStartTime.AddSeconds(1);

        var dir = NewSaveDir();
        persistence.WriteSnapshot(dir);
        persistence.PostWorldSave();
        return dir;
    }

    /// <summary>What FinishWorldSave does after a successful publish.</summary>
    private void Commit(string dir, SavePlan plan, DeltaPersistence persistence = null)
    {
        persistence ??= _persistence;
        persistence.CommitSave(plan.IsFull);
        persistence.SourcePath = Path.Combine(dir, persistence.Name, $"{persistence.Name}.bin");
        Assert.True(persistence.CopySourceValid);
    }

    private static void ResetSerializeCalls(DeltaPersistence persistence)
    {
        foreach (var entity in persistence.EntitiesBySerial.Values)
        {
            entity.SerializeCalls = 0;
        }
    }

    private (long Serialized, long Verified, long Copied, long Skipped) WorkerTotals()
    {
        var totals = (Serialized: 0L, Verified: 0L, Copied: 0L, Skipped: 0L);

        foreach (var worker in _workers)
        {
            totals.Serialized += worker.EntitiesSerialized;
            totals.Verified += worker.EntitiesVerified;
            totals.Copied += worker.EntitiesCopied;
            totals.Skipped += worker.EntitiesSkipped;
        }

        return totals;
    }

    private static int TotalSerializeCalls(DeltaPersistence persistence)
    {
        var total = 0;
        foreach (var entity in persistence.EntitiesBySerial.Values)
        {
            total += entity.SerializeCalls;
        }

        return total;
    }

    private void Populate(int count, Func<int, Serial, DeltaEntity> factory = null)
    {
        var rng = new System.Random(0x5EED);
        factory ??= static (_, serial) => new DeltaEntity(serial);

        for (var i = 1; i <= count; i++)
        {
            var serial = (Serial)(uint)i;
            var entity = factory(i, serial);
            entity.Value = rng.Next();
            entity.Name = i % 7 == 0 ? null : $"entity-{i}-{new string('x', i % 13)}";
            entity.SaveDirty = true;
            _persistence.EntitiesBySerial[serial] = entity;
            _persistence.RegisterType(entity.GetType());
        }
    }

    /// <summary>Reads a v5 idx + bin back into serial → record bytes.</summary>
    private static Dictionary<Serial, byte[]> ReadRecords(string dir, string name)
    {
        var idxPath = Path.Combine(dir, name, $"{name}.idx");
        var binPath = Path.Combine(dir, name, $"{name}.bin");
        var bin = File.ReadAllBytes(binPath);
        var idx = File.ReadAllBytes(idxPath);
        var reader = new BufferReader(idx);

        Assert.Equal(5, reader.ReadInt());
        reader.ReadLong(); // anchor

        var typeCount = reader.ReadInt();
        for (var i = 0; i < typeCount; i++)
        {
            reader.ReadStringRaw();
        }

        var count = reader.ReadInt();
        var records = new Dictionary<Serial, byte[]>(count);

        for (var i = 0; i < count; i++)
        {
            reader.ReadUShort(); // type index
            var serial = (Serial)reader.ReadUInt();
            reader.ReadLong(); // created
            var position = reader.ReadLong();
            var length = reader.ReadInt();

            records[serial] = bin.AsSpan((int)position, length).ToArray();
        }

        return records;
    }

    private static void AssertSameRecords(Dictionary<Serial, byte[]> expected, Dictionary<Serial, byte[]> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        foreach (var (serial, bytes) in expected)
        {
            Assert.True(actual.TryGetValue(serial, out var actualBytes), $"{serial} missing from the delta save");
            Assert.True(bytes.AsSpan().SequenceEqual(actualBytes), $"{serial} differs between delta and full save");
        }
    }

    // ---- tests -----------------------------------------------------------------------------

    [Fact]
    public void DeltaSave_CopiesCleanRecords_AndMatchesFullSaveOracle()
    {
        const int count = 20_000;
        Populate(count);

        var full = Save(FullPlan);
        Commit(full, FullPlan);
        Assert.Equal(count, TotalSerializeCalls(_persistence));

        foreach (var entity in _persistence.EntitiesBySerial.Values)
        {
            Assert.False(entity.SaveDirty);
            Assert.NotEqual(SavePlacement.None, entity.SavePlacement);
        }

        // Churn: mutate 3 % with a mark, delete 1 %, add 1 %.
        ResetSerializeCalls(_persistence);
        var rng = new System.Random(42);
        var expectedSerialized = 0;

        for (var i = 1; i <= count; i++)
        {
            var serial = (Serial)(uint)i;

            if (i % 100 == 0)
            {
                _persistence.EntitiesBySerial.Remove(serial);
                continue;
            }

            if (i % 33 == 0)
            {
                var entity = _persistence.EntitiesBySerial[serial];
                entity.Value = rng.Next();
                entity.Name = $"changed-{i}";
                entity.MarkDirty();
                expectedSerialized++;
            }
        }

        for (var i = count + 1; i <= count + count / 100; i++)
        {
            var serial = (Serial)(uint)i;
            _persistence.EntitiesBySerial[serial] = new DeltaEntity(serial) { Value = i, Name = $"new-{i}", SaveDirty = true };
            expectedSerialized++;
        }

        var delta = Save(DeltaPlan);
        Assert.Equal(expectedSerialized, TotalSerializeCalls(_persistence));
        Assert.False(DeltaSaves.Report.HasViolations);
        Assert.Equal(count - count / 100 - expectedSerialized + count / 100, WorkerTotals().Copied);
        Assert.Equal(WorkerTotals().Copied, DeltaSaves.Report.Copied);

        // Oracle: the same frozen state serialized in full.
        var oracle = Save(FullPlan);
        AssertSameRecords(ReadRecords(oracle, "Delta"), ReadRecords(delta, "Delta"));

        // And it loads.
        var loaded = new DeltaPersistence(_priority++);
        try
        {
            loaded.DeserializeIndexes(delta, null);
            loaded.Deserialize(delta, null);

            Assert.Equal(_persistence.EntitiesBySerial.Count, loaded.EntitiesBySerial.Count);

            foreach (var (serial, original) in _persistence.EntitiesBySerial)
            {
                var entity = loaded.EntitiesBySerial[serial];
                Assert.Equal(original.Value, entity.Value);
                Assert.Equal(original.Name, entity.Name);
                Assert.Equal(SavePlacement.None, entity.SavePlacement);
            }
        }
        finally
        {
            loaded.Unregister();
        }
    }

    [Fact]
    public void DeltaSave_ChainsAcrossSaves_PlacementsFollowTheNewestFile()
    {
        Populate(5_000);

        var save1 = Save(FullPlan);
        Commit(save1, FullPlan);

        // Delta 1: change a few, commit; delta 2 copies from delta 1's file, not from the full.
        _persistence.EntitiesBySerial[(Serial)7u].Value = 1;
        _persistence.EntitiesBySerial[(Serial)7u].MarkDirty();
        var save2 = Save(DeltaPlan);
        Commit(save2, DeltaPlan);

        _persistence.EntitiesBySerial[(Serial)9u].Value = 2;
        _persistence.EntitiesBySerial[(Serial)9u].MarkDirty();
        ResetSerializeCalls(_persistence);
        var save3 = Save(DeltaPlan);
        Assert.Equal(1, TotalSerializeCalls(_persistence));

        var oracle = Save(FullPlan);
        AssertSameRecords(ReadRecords(oracle, "Delta"), ReadRecords(save3, "Delta"));
    }

    [Fact]
    public void VerifyMode_ReportsMutationsWithoutMarkDirty_AndWritesFreshBytes()
    {
        Populate(3_000);
        var full = Save(FullPlan);
        Commit(full, FullPlan);

        for (var i = 1; i <= 5; i++)
        {
            _persistence.EntitiesBySerial[(Serial)(uint)(i * 100)].Value = -i; // no MarkDirty
        }

        var verified = Save(FullVerifyPlan);
        var report = DeltaSaves.Report;

        Assert.True(report.HasViolations);
        Assert.Equal(5, report.ViolationCounts[typeof(DeltaEntity)]);
        Assert.Equal(5, report.ViolationSerials(typeof(DeltaEntity)).Count);

        // Every entity was clean, placed and trusted, so every one was verified; five failed.
        Assert.Equal(3_000, WorkerTotals().Verified);

        var oracle = Save(FullPlan);
        AssertSameRecords(ReadRecords(oracle, "Delta"), ReadRecords(verified, "Delta"));
    }

    [Fact]
    public void OnMode_SampleCatchesMutationWithoutMarkDirty()
    {
        Populate(3_000);
        var full = Save(FullPlan);
        Commit(full, FullPlan);

        _persistence.EntitiesBySerial[(Serial)250u].Name = "stale"; // no MarkDirty

        ResetSerializeCalls(_persistence);
        Save(DeltaSampleAllPlan);

        Assert.Equal(3_000, TotalSerializeCalls(_persistence)); // rate 1: everything is sampled
        Assert.Equal(1, DeltaSaves.Report.ViolationCounts[typeof(DeltaEntity)]);
        Assert.Equal((Serial)250u, DeltaSaves.Report.ViolationSerials(typeof(DeltaEntity))[0]);
    }

    [Fact]
    public void UntrustedType_AlwaysSerializes_TrustedTypeIsCopied()
    {
        Populate(2_000, static (i, serial) => i % 2 == 0 ? new UntrustedDeltaEntity(serial) : new DeltaEntity(serial));

        Assert.True(DeltaSaves.IsEligible(typeof(DeltaEntity)));
        Assert.False(DeltaSaves.IsEligible(typeof(UntrustedDeltaEntity)));

        var full = Save(FullPlan);
        Commit(full, FullPlan);

        ResetSerializeCalls(_persistence);
        Save(DeltaPlan);

        foreach (var entity in _persistence.EntitiesBySerial.Values)
        {
            Assert.Equal(entity is UntrustedDeltaEntity ? 1 : 0, entity.SerializeCalls);
        }
    }

    [Fact]
    public void DenylistedType_IsNotCopied_UntilTrustedAgain()
    {
        Populate(1_000);
        var full = Save(FullPlan);
        Commit(full, FullPlan);

        Assert.True(DeltaSaves.Deny(typeof(DeltaEntity)));
        Assert.False(_persistence.IsTrustedType(typeof(DeltaEntity)));

        ResetSerializeCalls(_persistence);
        Save(DeltaPlan);
        Assert.Equal(1_000, TotalSerializeCalls(_persistence));

        Assert.True(DeltaSaves.Trust(typeof(DeltaEntity).FullName));
        Assert.True(_persistence.IsTrustedType(typeof(DeltaEntity)));
    }

    [Fact]
    public void SkippedEntities_AreNeitherSerializedNorWritten()
    {
        Populate(500, static (i, serial) => i % 5 == 0 ? new SkippedDeltaEntity(serial) : new DeltaEntity(serial));

        var full = Save(FullPlan);
        var records = ReadRecords(full, "Delta");

        Assert.Equal(400, records.Count);
        Assert.Equal(100, WorkerTotals().Skipped);

        foreach (var entity in _persistence.EntitiesBySerial.Values)
        {
            if (entity is SkippedDeltaEntity)
            {
                Assert.Equal(0, entity.SerializeCalls);
                Assert.Equal(SavePlacement.None, entity.SavePlacement);
            }
            else
            {
                Assert.Equal(1, entity.SerializeCalls);
            }
        }
    }

    [Fact]
    public void ADifferentSaveWithTheSameSize_IsNotAcceptedAsCopySource()
    {
        Populate(1_000);
        var save1 = Save(FullPlan);
        Commit(save1, FullPlan);

        // Another full save of identical content: same length, and the file times can match
        // to the tick on a fast disk. Point the placements at it and force the times equal.
        var save2 = Save(FullPlan);
        var bin1 = _persistence.SourcePath;
        var bin2 = Path.Combine(save2, "Delta", "Delta.bin");
        File.SetLastWriteTimeUtc(bin2, File.GetLastWriteTimeUtc(bin1));
        _persistence.SourcePath = bin2;

        Assert.False(_persistence.CopySourceValid);
    }

    [Fact]
    public void ASerializerThrowing_IsReportedByTheWorker_NotThrown()
    {
        Populate(50, static (i, serial) => i == 25 ? new ThrowingDeltaEntity(serial) : new DeltaEntity(serial));
        DeltaSaves.SetPlanForTest(FullPlan);

        foreach (var worker in _workers)
        {
            worker.Wake();
        }

        _source.SetOwner(_persistence);
        _source.PushSingle(_persistence);
        Assert.True(_persistence.TrySnapshotEntries(out var slotCount));
        _source.PushSlotRanges(_persistence, slotCount);
        _source.Flush();

        foreach (var worker in _workers)
        {
            worker.Sleep();
        }

        Exception error = null;
        foreach (var worker in _workers)
        {
            error ??= worker.Error;
        }

        Assert.IsType<InvalidOperationException>(error);
        _persistence.PostWorldSave();
    }

    [Fact]
    public void ZeroLengthRecord_KeepsItsIndexEntry()
    {
        Populate(3, static (i, serial) => i == 2 ? new EmptyDeltaEntity(serial) : new DeltaEntity(serial));

        var full = Save(FullPlan);
        var records = ReadRecords(full, "Delta");

        Assert.Equal(3, records.Count);
        Assert.Empty(records[(Serial)2u]);
        Assert.Equal(SavePlacement.None, _persistence.EntitiesBySerial[(Serial)2u].SavePlacement);
    }

    [Fact]
    public void ChangedCopySource_IsDetectedBeforeTheFreeze_AndRejectedByTheWriter()
    {
        Populate(1_000);
        var full = Save(FullPlan);
        Commit(full, FullPlan);

        // Something replaced the file behind our back (restored backup, hand edit).
        var bin = _persistence.SourcePath;
        File.WriteAllBytes(bin, File.ReadAllBytes(bin)[..^1]);

        Assert.False(_persistence.CopySourceValid);
        Assert.Throws<InvalidOperationException>(() => Save(DeltaPlan));
    }

    [Fact]
    public void FreedSerials_AreNotReissued_UntilAFullSaveCommits()
    {
        Populate(10);
        var full = Save(FullPlan);
        Commit(full, FullPlan);

        var victim = _persistence.EntitiesBySerial[(Serial)5u];
        _persistence.RemoveEntity(victim);
        Assert.Equal(1, _persistence.UnreusableSerialCount);

        var delta = Save(DeltaPlan);
        Commit(delta, DeltaPlan);
        Assert.Equal(1, _persistence.UnreusableSerialCount);

        var full2 = Save(FullPlan);
        Commit(full2, FullPlan);
        Assert.Equal(0, _persistence.UnreusableSerialCount);
    }

    [Fact]
    public void SerialsFreedAfterAFullFreeze_SurviveThatSaveCommitting()
    {
        Populate(10);
        var full = Save(FullPlan);
        Commit(full, FullPlan);

        _persistence.RemoveEntity(_persistence.EntitiesBySerial[(Serial)3u]);

        // A full save freezes (the guard set rotates aside), then an entity is freed while the
        // writer runs, then the save commits: only the pre-freeze serial may be dropped.
        var full2 = Save(FullPlan);
        _persistence.RemoveEntity(_persistence.EntitiesBySerial[(Serial)4u]);
        Assert.Equal(2, _persistence.UnreusableSerialCount);

        Commit(full2, FullPlan);
        Assert.Equal(1, _persistence.UnreusableSerialCount);
    }

    [Fact]
    public void FailedSave_ForgetsPlacements_AndTheRecoverySaveVerifiesNothing()
    {
        Populate(2_000);
        var full = Save(FullPlan);
        Commit(full, FullPlan);

        _persistence.RemoveEntity(_persistence.EntitiesBySerial[(Serial)2u]);
        var guarded = _persistence.UnreusableSerialCount;

        // A delta save whose publish fails: the writer already rewrote placements into the
        // file that never became the save.
        _persistence.EntitiesBySerial[(Serial)1u].Value = 1;
        _persistence.EntitiesBySerial[(Serial)1u].MarkDirty();
        Save(DeltaPlan);
        _persistence.FailSave();

        Assert.False(_persistence.CopySourceValid);
        Assert.Equal(guarded, _persistence.UnreusableSerialCount);

        // The recovery save is full with verification on; stale placements must not be used
        // for comparison (they would be out of range or compare against the wrong bytes).
        var recovery = Save(FullVerifyPlan);
        Assert.False(DeltaSaves.Report.HasViolations);
        Assert.Equal(0, WorkerTotals().Copied);

        var oracle = Save(FullPlan);
        AssertSameRecords(ReadRecords(oracle, "Delta"), ReadRecords(recovery, "Delta"));
    }
}

public class SavePlacementTests
{
    [Theory]
    [InlineData(0L, 1)]
    [InlineData(0L, SavePlacement.MaxLength)]
    [InlineData(123_456_789_012L, 4096)]
    [InlineData((1L << 40) - 3, 7)]
    public void RoundTrips(long position, int length)
    {
        var placement = SavePlacement.Encode(position, length);

        Assert.False(SavePlacement.IsNone(placement));
        Assert.Equal(position, SavePlacement.Position(placement));
        Assert.Equal(length, SavePlacement.Length(placement));
    }

    [Theory]
    [InlineData(0L, 0)]
    [InlineData(0L, SavePlacement.MaxLength + 1)]
    [InlineData(-1L, 10)]
    [InlineData(1L << 40, 10)]
    public void OutOfRange_IsNone(long position, int length) =>
        Assert.True(SavePlacement.IsNone(SavePlacement.Encode(position, length)));
}

public class SaveSamplerTests
{
    [Fact]
    public void RateZero_NeverHits_RateOne_AlwaysHits()
    {
        var sampler = new SaveSampler();
        sampler.Reseed(3, 1);

        for (var i = 0; i < 1000; i++)
        {
            Assert.False(sampler.Hit(SaveSampler.Threshold(0)));
            Assert.True(sampler.Hit(SaveSampler.Threshold(1)));
        }
    }

    [Fact]
    public void Rate_IsApproximatelyHonoured_AndReproducible()
    {
        var a = new SaveSampler();
        var b = new SaveSampler();
        a.Reseed(7, 2);
        b.Reseed(7, 2);

        var threshold = SaveSampler.Threshold(0.1);
        var hits = 0;

        for (var i = 0; i < 100_000; i++)
        {
            var hit = a.Hit(threshold);
            Assert.Equal(hit, b.Hit(threshold));
            hits += hit ? 1 : 0;
        }

        Assert.InRange(hits, 9_000, 11_000);
    }
}

public class DeltaEligibilityTests
{
    [AuditedDirtyTracking("t")]
    private class AuditedRoot : ISerializable
    {
        public Serial Serial => Serial.Zero;
        public DateTime Created { get; set; }
        public bool Deleted => false;
        public void Delete() { }
        public void Serialize(IGenericWriter writer) { }
        public void Deserialize(IGenericReader reader) { }
    }

    [AuditedDirtyTracking("t")]
    private class AuditedChild : AuditedRoot
    {
    }

    private class UnauditedChild : AuditedRoot
    {
    }

    [AuditedDirtyTracking("t")]
    private class GrandChildOfUnaudited : UnauditedChild
    {
    }

    [ModernUO.Serialization.VolatileSerializedState(ModernUO.Serialization.VolatileReason.Declared)]
    [AuditedDirtyTracking("t")]
    private class VolatileChild : AuditedRoot
    {
    }

    private class UnauditedRoot : ISerializable
    {
        public Serial Serial => Serial.Zero;
        public DateTime Created { get; set; }
        public bool Deleted => false;
        public void Delete() { }
        public void Serialize(IGenericWriter writer) { }
        public void Deserialize(IGenericReader reader) { }
    }

    [AuditedDirtyTracking("t")]
    private class AuditedChildOfUnauditedRoot : UnauditedRoot
    {
    }

    [Theory]
    [InlineData(typeof(AuditedRoot), true)]
    [InlineData(typeof(AuditedChild), true)]
    [InlineData(typeof(UnauditedChild), false)]
    [InlineData(typeof(GrandChildOfUnaudited), false)]
    [InlineData(typeof(VolatileChild), false)]
    [InlineData(typeof(UnauditedRoot), false)]
    [InlineData(typeof(AuditedChildOfUnauditedRoot), false)]
    public void EveryClassInTheChainMustQualify(Type type, bool eligible) =>
        Assert.Equal(eligible, DeltaSaves.IsEligible(type));
}
