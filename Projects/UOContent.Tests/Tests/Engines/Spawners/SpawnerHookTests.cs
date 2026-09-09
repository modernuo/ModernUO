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
            TestEntry te;
            if (e is TestEntry existing)
            {
                te = existing;
            }
            else
            {
                // CloneEntry does not copy spawns, so a converted entry would orphan its creatures.
                te = (TestEntry)CloneEntry(e);
                TransferSpawned(e, te);
            }

            te.SetParent(this);
            AddToTestEntries(te);
        }
    }

    /// <summary>Test hook: adopt entries built elsewhere, exercising the conversion path.</summary>
    public void AdoptForTest(IReadOnlyList<SpawnerEntry> entries) => AdoptEntries(entries);

    /// <summary>Test hook: rebuild the Spawned registry without a full save round trip.</summary>
    public void RebuildSpawnedForTest() => RebuildSpawned();

    // The base class rebuilds Spawned from Spawner's own (empty) list before _testEntries has been
    // read, so this owner has to rebuild again once its list exists.
    [AfterDeserialization]
    private void AfterDeserialization() => RebuildSpawned();

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
        spawner.Spawn();
        Assert.Single(spawner.Spawned);

        var loaded = SpawnerBlob.Read<HookRecordingSpawner>(SpawnerBlob.Write(spawner), (Serial)0x40009999);
        Assert.Equal("kept", ((TestEntry)loaded.Entries[0]).Tag);

        // The owner's [AfterDeserialization] has to rebuild Spawned from its own list: the base
        // class's runs before _testEntries has been read.
        Assert.Single(loaded.Entries[0].Spawned);
        Assert.Single(loaded.Spawned);

        var copy = new HookRecordingSpawner();
        spawner.Dupe(copy);
        Assert.Equal("kept", ((TestEntry)copy.Entries[0]).Tag);
        Assert.Empty(copy.Entries[0].Spawned);

        // `loaded` shares the live creature with `spawner`; deleting the spawner deletes it.
        spawner.Delete();
        loaded.Delete();
        copy.Delete();
    }

    [Fact]
    public void AdoptEntries_ConvertsForeignEntries_KeepingLiveSpawns()
    {
        var source = new Spawner();
        source.InitSpawn(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10));
        source.MoveToWorld(new Point3D(1502, 1502, 0), Map.Felucca);
        source.AddEntry("Rabbit", 100, 1, false);
        source.Spawn();
        var rabbit = Assert.Single(source.Spawned).Key;

        var adopter = Place();
        adopter.AdoptForTest(source.Entries);

        var adopted = Assert.Single(adopter.Entries);
        Assert.IsType<TestEntry>(adopted);
        Assert.Equal("Rabbit", adopted.SpawnedName);
        Assert.Same(rabbit, Assert.Single(adopted.Spawned));
        Assert.Empty(source.Entries[0].Spawned);

        adopter.RebuildSpawnedForTest();
        Assert.Same(rabbit, Assert.Single(adopter.Spawned).Key);

        // The adopter owns the creature now, so deleting the source must leave it alone.
        source.Delete();
        Assert.False(rabbit.Deleted);

        adopter.Delete();
        Assert.True(rabbit.Deleted);
    }
}
