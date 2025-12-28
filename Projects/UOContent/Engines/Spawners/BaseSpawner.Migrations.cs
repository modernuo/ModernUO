using System.Collections.Generic;
using Server.Items;

namespace Server.Engines.Spawners;

public abstract partial class BaseSpawner
{
    // Static dictionary to track pending HomeRange migrations from v10
    // Keyed by spawner instance, stores the HomeRange value to convert to SpawnBounds
    private static readonly Dictionary<BaseSpawner, int> _pendingHomeRangeMigrations = new();

    private void MigrateFrom(V10Content content)
    {
        _guid = content.Guid;
        _returnOnDeactivate = content.ReturnOnDeactivate;
        _entries = content.Entries;
        _walkingRange = content.WalkingRange;
        _wayPoint = content.WayPoint;
        _group = content.Group;
        _minDelay = content.MinDelay;
        _maxDelay = content.MaxDelay;
        _count = content.Count;
        _team = content.Team;
        _running = content.Running;
        _end = _running ? content.End : Core.Now;

        // Defer SpawnBounds calculation to AfterDeserialization when Location is available
        if (content.HomeRange > 0)
        {
            _pendingHomeRangeMigrations[this] = content.HomeRange;
        }

        _spawnLocationIsHome = false;

        // New v12 fields default to automatic/default
        _spawnPositionMode = SpawnPositionMode.Automatic;
        _maxSpawnAttempts = DefaultMaxSpawnAttempts;
    }

    private void MigrateFrom(V11Content content)
    {
        _guid = content.Guid;
        _returnOnDeactivate = content.ReturnOnDeactivate;
        _entries = content.Entries;
        _walkingRange = content.WalkingRange;
        _wayPoint = content.WayPoint;
        _group = content.Group;
        _minDelay = content.MinDelay;
        _maxDelay = content.MaxDelay;
        _count = content.Count;
        _team = content.Team;

        // Moved to Spawner
        if (this is Spawner spawner)
        {
            spawner.SpawnBounds = content.SpawnBounds;
        }

        _running = content.Running;
        _spawnLocationIsHome = content.SpawnLocationIsHome;
        _end = _running ? content.End : Core.Now;

        // New v12 fields default to automatic/default
        _spawnPositionMode = SpawnPositionMode.Automatic;
        _maxSpawnAttempts = DefaultMaxSpawnAttempts;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _guid = reader.ReadGuid();
        _returnOnDeactivate = reader.ReadBool();

        var count = reader.ReadInt();
        _entries = new List<SpawnerEntry>(count);

        for (var i = 0; i < count; ++i)
        {
            var entry = new SpawnerEntry(this);
            entry.Deserialize(reader);
            _entries.Add(entry);
        }

        _walkingRange = reader.ReadInt();
        _wayPoint = reader.ReadEntity<WayPoint>();
        _group = reader.ReadBool();
        _minDelay = reader.ReadTimeSpan();
        _maxDelay = reader.ReadTimeSpan();
        _count = reader.ReadInt();
        _team = reader.ReadInt();

        // Store homeRange for migration in AfterDeserialization when Location is available
        var homeRange = reader.ReadInt();
        if (homeRange > 0)
        {
            _pendingHomeRangeMigrations[this] = homeRange;
        }

        _running = reader.ReadBool();
        _end = _running ? reader.ReadDeltaTime() : Core.Now;
    }
}
