using System;
using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Engines.Spawners;

public abstract partial class BaseSpawner
{
    /// <summary>
    /// The entries this spawner cycles through, owned by the concrete subclass so it can use its own
    /// entry type. Cold read-only view; loops inside BaseSpawner use <see cref="EntrySpan"/>.
    /// </summary>
    [IgnoreDupe]
    public abstract IReadOnlyList<SpawnerEntry> Entries { get; }

    /// <summary>Zero-cost span over the owner's list for hot loops (no interface dispatch, no allocation).</summary>
    protected abstract ReadOnlySpan<SpawnerEntry> EntrySpan { get; }

    /// <summary>Creates an entry of the owner's entry type, parented to this spawner. Not added.</summary>
    protected abstract SpawnerEntry CreateEntry(
        string name,
        int probability,
        int maxCount,
        string properties,
        string parameters
    );

    protected abstract void AddEntryCore(SpawnerEntry entry);

    protected abstract bool RemoveEntryCore(SpawnerEntry entry);

    protected abstract void ClearEntriesCore();

    /// <summary>
    /// Takes ownership of entries built elsewhere (a legacy save, a DTO import). The owner stores
    /// them, re-parents them, and converts foreign entry types if it must. Replaces the current list.
    /// </summary>
    protected abstract void AdoptEntries(IReadOnlyList<SpawnerEntry> entries);

    /// <summary>Deep-copies an entry into this spawner's entry type. Override to carry subtype fields.</summary>
    protected virtual SpawnerEntry CloneEntry(SpawnerEntry source)
    {
        var entry = CreateEntry(
            source.SpawnedName,
            source.SpawnedProbability,
            source.SpawnedMaxCount,
            source.Properties,
            source.Parameters
        );
        entry.Disabled = source.Disabled;
        return entry;
    }

    public SpawnerEntry AddEntry(
        string creaturename,
        int probability = 100,
        int amount = 1,
        bool dotimer = true,
        string properties = null,
        string parameters = null
    )
    {
        var entry = CreateEntry(creaturename, probability, amount, properties, parameters);
        AddEntryCore(entry);
        if (dotimer)
        {
            DoTimer(TimeSpan.FromSeconds(1));
        }

        return entry;
    }

    public void RemoveEntry(SpawnerEntry entry)
    {
        Defrag();
        RemoveSpawn(entry);
        RemoveEntryCore(entry);

        if (_running && !IsFull && _timer?.Running != true)
        {
            DoTimer();
        }

        InvalidateProperties();
    }

    /// <summary>Deletes every live spawn and removes every entry.</summary>
    public void ClearEntries()
    {
        RemoveSpawns();
        ClearEntriesCore();
        InvalidateProperties();
    }

    /// <summary>Replaces <paramref name="target"/>'s entries with clones of this spawner's entries.</summary>
    public void CopyEntriesTo(BaseSpawner target)
    {
        target.ClearEntries();

        var entries = EntrySpan;
        for (var i = 0; i < entries.Length; i++)
        {
            target.AddEntryCore(target.CloneEntry(entries[i]));
        }

        target.InvalidateProperties();
    }

    /// <summary>
    /// Rebuilds the entity -> entry registry from the owner's entries and re-arms the timer.
    /// The owner calls this from its own [AfterDeserialization] once its list is loaded; the base
    /// hook runs before derived fields exist and must not touch entries.
    /// </summary>
    protected void RebuildSpawned()
    {
        Spawned = new Dictionary<ISpawnable, SpawnerEntry>();

        var entries = EntrySpan;
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            var spawned = entry.Spawned;
            for (var j = 0; j < spawned.Count; j++)
            {
                Spawned.TryAdd(spawned[j], entry);
            }
        }

        DoTimer(_end - Core.Now);
    }
}
