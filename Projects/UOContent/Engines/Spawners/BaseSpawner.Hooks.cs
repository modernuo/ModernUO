namespace Server.Engines.Spawners;

public abstract partial class BaseSpawner
{
    /// <summary>
    /// Called after the timer starts (Start(), Running = true, NextSpawn on a stopped spawner).
    /// Not called for construction (<c>InitSpawn</c>) or deserialization; subclasses initialise
    /// run state in their constructor and <c>[AfterDeserialization]</c>.
    /// </summary>
    protected virtual void OnStarted()
    {
    }

    /// <summary>Called after the timer stops (Stop(), Running = false), and only if it was running.</summary>
    protected virtual void OnStopped()
    {
    }

    /// <summary>Veto point before an entry's entity is constructed. Return false to skip this attempt.</summary>
    protected virtual bool OnBeforeSpawn(SpawnerEntry entry) => true;

    /// <summary>
    /// Runs after property application and before positioning, so computed properties apply first.
    /// The entity is not yet in <see cref="Spawned"/>, has no <c>Spawner</c> set, and is still on
    /// the internal map.
    /// </summary>
    protected virtual void OnConfigureSpawned(SpawnerEntry entry, ISpawnable spawned)
    {
    }

    /// <summary>Entry-aware positioning. Default delegates to the entry-agnostic overload.</summary>
    protected virtual Point3D GetSpawnPosition(SpawnerEntry entry, ISpawnable spawned, Map map) =>
        GetSpawnPosition(spawned, map);

    /// <summary>Runs after the entity is in the world and linked to this spawner.</summary>
    protected virtual void OnSpawned(SpawnerEntry entry, ISpawnable spawned)
    {
    }

    /// <summary>A spawned creature died (before base death deletes it and unlinks the spawner).</summary>
    protected virtual void OnSpawnedDeath(SpawnerEntry entry, ISpawnable spawned, Mobile killer)
    {
    }

    /// <summary>Entry point for BaseCreature.OnDeath. Resolves the entry and dispatches the hook.</summary>
    public void NotifySpawnedDeath(ISpawnable spawned, Mobile killer)
    {
        if (spawned != null && Spawned != null && Spawned.TryGetValue(spawned, out var entry))
        {
            OnSpawnedDeath(entry, spawned, killer);
        }
    }
}
