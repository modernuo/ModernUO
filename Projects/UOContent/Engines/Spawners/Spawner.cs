using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ModernUO.Serialization;

namespace Server.Engines.Spawners;

[SerializationGenerator(2)]
public partial class Spawner : BaseSpawner
{
    /// <summary>
    /// When true, enables proactive spiral scanning to find valid spawn positions.
    /// Only relevant when SpawnPositionMode is Automatic or Enabled.
    /// </summary>
    private bool ShouldSerializeUseSpiralScan() => _useSpiralScan;

    [SerializableField(0)]
    [SaveFlag(nameof(ShouldSerializeUseSpiralScan))]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private bool _useSpiralScan;

    private bool ShouldSerializeSpawnBounds() => _spawnBounds != default;

    [SerializableProperty(1)]
    [SaveFlag(nameof(ShouldSerializeSpawnBounds))]
    [CommandProperty(AccessLevel.Developer)]
    public override Rectangle3D SpawnBounds
    {
        get => _spawnBounds;
        set
        {
            _spawnBounds = value;
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    // Owned here (v2) rather than on BaseSpawner so subclasses can store their own entry type.
    [SerializedIgnoreDupe]
    [SerializableField(2, getter: "protected", setter: "private")]
    private List<SpawnerEntry> _entryList = [];

    [Constructible(AccessLevel.Developer)]
    public Spawner()
    {
    }

    [Constructible(AccessLevel.Developer)]
    public Spawner(string spawnedName) : base(spawnedName)
    {
    }

    [Constructible(AccessLevel.Developer)]
    public Spawner(
        int amount,
        TimeSpan minDelay,
        TimeSpan maxDelay,
        int team = 0,
        Rectangle3D spawnBounds = default,
        params ReadOnlySpan<string> spawnedNames
    ) : base(amount, minDelay, maxDelay, team, spawnBounds, spawnedNames)
    {
    }

    public override Region Region => Region.Find(Location, Map);

    protected override bool SupportsSpiralScan => _useSpiralScan;

    protected override Rectangle3D GetBoundsForSpawnAttempt() => SpawnBounds;

    protected override ReadOnlySpan<Rectangle3D> GetAllSpawnBounds() => new(ref _spawnBounds);

    public override IReadOnlyList<SpawnerEntry> Entries => _entryList;

    protected override ReadOnlySpan<SpawnerEntry> EntrySpan => CollectionsMarshal.AsSpan(_entryList);

    protected override SpawnerEntry CreateEntry(
        string name,
        int probability,
        int maxCount,
        string properties,
        string parameters
    ) => new(this, name, probability, maxCount, properties, parameters);

    protected override void AddEntryCore(SpawnerEntry entry) => AddToEntryList(entry);

    protected override bool RemoveEntryCore(SpawnerEntry entry)
    {
        if (!_entryList.Contains(entry))
        {
            return false;
        }

        RemoveFromEntryList(entry);
        return true;
    }

    protected override void ClearEntriesCore() => ClearEntryList();

    protected override void AdoptEntries(IReadOnlyList<SpawnerEntry> entries)
    {
        // Always copy: taking ownership of a caller's List<SpawnerEntry> would alias it, so a later
        // mutation on either side would silently show up on the other.
        var list = new List<SpawnerEntry>(entries);
        for (var i = 0; i < list.Count; i++)
        {
            list[i].SetParent(this);
        }

        EntryList = list;
    }

    private void MigrateFrom(V0Content content)
    {
        // V0 had no fields in Spawner; v1 added _useSpiralScan (false), v2 owns the entry list,
        // which BaseSpawner's migration hands over through AdoptEntries.
    }

    private void MigrateFrom(V1Content content)
    {
        _useSpiralScan = content.UseSpiralScan;
        _spawnBounds = content.SpawnBounds ?? default;
        // _entryList was adopted by BaseSpawner.MigrateFrom(V12Content) before this ran.
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _entryList ??= [];
        RebuildSpawned();
    }
}
