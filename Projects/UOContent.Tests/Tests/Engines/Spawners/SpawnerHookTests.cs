using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ModernUO.Serialization;
using Server;
using Server.Engines.Spawners;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Engines.Spawners;

[SerializationGenerator(0)]
public partial class TestEntry : SpawnerEntry
{
    // The generator only looks at the declared type for dirty tracking, so a derived entry must
    // re-declare the owning spawner even though SpawnerEntry already tracks it.
    [DirtyTrackingEntity]
    private BaseSpawner Owner => Parent;

    [SerializableField(0)]
    private string _tag;

    public TestEntry(BaseSpawner parent) : base(parent)
    {
    }

    public TestEntry(BaseSpawner parent, string name, int probability, int maxCount, string properties, string parameters)
        : base(parent, name, probability, maxCount, properties, parameters)
    {
    }
}

[SerializationGenerator(0)]
public partial class HookRecordingSpawner : Spawner
{
    [SerializedIgnoreDupe]
    [SerializableField(0)]
    private List<TestEntry> _testEntries = [];

    public List<string> Log { get; } = [];
    public bool VetoNext { get; set; }

    public HookRecordingSpawner()
    {
    }

    public HookRecordingSpawner(Serial serial) : base(serial)
    {
    }

    public override IReadOnlyList<SpawnerEntry> Entries => _testEntries;

    protected override ReadOnlySpan<SpawnerEntry> EntrySpan =>
        ReadOnlySpan<SpawnerEntry>.CastUp(CollectionsMarshal.AsSpan(_testEntries));

    protected override SpawnerEntry CreateEntry(string name, int probability, int maxCount, string properties, string parameters) =>
        new TestEntry(this, name, probability, maxCount, properties, parameters) { Tag = "made" };

    protected override void AddEntryCore(SpawnerEntry entry) => AddToTestEntries((TestEntry)entry);

    protected override bool RemoveEntryCore(SpawnerEntry entry)
    {
        if (entry is not TestEntry te || !_testEntries.Contains(te))
        {
            return false;
        }

        RemoveFromTestEntries(te);
        return true;
    }

    protected override void ClearEntriesCore() => ClearTestEntries();

    protected override void AdoptEntries(IReadOnlyList<SpawnerEntry> entries)
    {
        ClearTestEntries();
        for (var i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            var te = e as TestEntry ?? (TestEntry)CloneEntry(e);
            te.SetParent(this);
            AddToTestEntries(te);
        }
    }

    protected override SpawnerEntry CloneEntry(SpawnerEntry source)
    {
        var clone = (TestEntry)base.CloneEntry(source);
        clone.Tag = (source as TestEntry)?.Tag ?? clone.Tag;
        return clone;
    }

    protected override void OnStarted() => Log.Add("started");
    protected override void OnStopped() => Log.Add("stopped");

    protected override bool OnBeforeSpawn(SpawnerEntry entry)
    {
        Log.Add($"before:{entry.SpawnedName}");
        if (VetoNext)
        {
            VetoNext = false;
            return false;
        }

        return true;
    }

    protected override void OnConfigureSpawned(SpawnerEntry entry, ISpawnable spawned) => Log.Add($"configure:{entry.SpawnedName}");

    protected override Point3D GetSpawnPosition(SpawnerEntry entry, ISpawnable spawned, Map map)
    {
        Log.Add($"position:{entry.SpawnedName}");
        return Location;
    }

    protected override void OnSpawned(SpawnerEntry entry, ISpawnable spawned) => Log.Add($"spawned:{entry.SpawnedName}");

    protected override void OnSpawnedDeath(SpawnerEntry entry, ISpawnable spawned, Mobile killer) =>
        Log.Add($"death:{entry.SpawnedName}:{killer?.Name ?? "none"}");
}

[Collection("Sequential UOContent Tests")]
public class SpawnerHookTests
{
    private static HookRecordingSpawner Place()
    {
        var spawner = new HookRecordingSpawner();
        spawner.InitSpawn(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10));
        spawner.MoveToWorld(new Point3D(1500, 1500, 0), Map.Felucca);
        return spawner;
    }

    [Fact]
    public void Hooks_FireInOrder_OnSpawnAndStartStop()
    {
        var spawner = Place();
        spawner.AddEntry("Rabbit", 100, 1, false);
        spawner.Log.Clear();

        spawner.Stop();
        spawner.Start();
        spawner.Spawn();

        Assert.Equal(
            ["stopped", "started", "before:Rabbit", "configure:Rabbit", "position:Rabbit", "spawned:Rabbit"],
            spawner.Log
        );
        Assert.Single(spawner.Spawned);

        spawner.Delete();
    }

    [Fact]
    public void Hooks_FireForItemEntries()
    {
        var spawner = Place();
        spawner.AddEntry("Gold", 100, 1, false);
        spawner.Log.Clear();

        spawner.Spawn();

        Assert.Equal(
            ["before:Gold", "configure:Gold", "position:Gold", "spawned:Gold"],
            spawner.Log
        );
        Assert.IsAssignableFrom<Item>(Assert.Single(spawner.Spawned).Key);

        spawner.Delete();
    }

    [Fact]
    public void NextSpawn_OnStoppedSpawner_FiresOnStarted()
    {
        var spawner = Place();
        spawner.AddEntry("Rabbit", 100, 1, false);
        spawner.Stop();
        spawner.Log.Clear();

        spawner.NextSpawn = TimeSpan.FromSeconds(5);

        Assert.True(spawner.Running);
        Assert.Equal(["started"], spawner.Log);

        spawner.Delete();
    }

    [Fact]
    public void OnBeforeSpawn_CanVeto()
    {
        var spawner = Place();
        spawner.AddEntry("Rabbit", 100, 1, false);
        spawner.VetoNext = true;

        spawner.Spawn();

        Assert.Empty(spawner.Spawned);
        Assert.Contains("before:Rabbit", spawner.Log);
        Assert.DoesNotContain("spawned:Rabbit", spawner.Log);

        spawner.Delete();
    }

    [Fact]
    public void Death_NotifiesOwningSpawner_BeforeUnlink()
    {
        var spawner = Place();
        spawner.AddEntry("Rabbit", 100, 1, false);
        spawner.Spawn();
        var rabbit = Assert.Single(spawner.Spawned).Key as BaseCreature;
        Assert.NotNull(rabbit);

        var killer = new PlayerMobile { Name = "Hunter" };
        rabbit.LastKiller = killer;
        rabbit.Kill();

        Assert.Contains("death:Rabbit:Hunter", spawner.Log);

        killer.Delete();
        spawner.Delete();
    }

    [Fact]
    public void OwnEntryType_SurvivesBinaryRoundTripAndDupe()
    {
        var spawner = Place();
        spawner.AddEntry("Rabbit", 100, 1, false);
        ((TestEntry)spawner.Entries[0]).Tag = "kept";

        var loaded = SpawnerBlob.Read<HookRecordingSpawner>(SpawnerBlob.Write(spawner), (Serial)0x40009999);
        Assert.Equal("kept", ((TestEntry)loaded.Entries[0]).Tag);

        var copy = new HookRecordingSpawner();
        spawner.Dupe(copy);
        Assert.Equal("kept", ((TestEntry)copy.Entries[0]).Tag);

        spawner.Delete();
        loaded.Delete();
        copy.Delete();
    }
}
