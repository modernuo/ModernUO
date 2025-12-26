using System;
using System.Collections.Generic;
using System.Text.Json;
using ModernUO.Serialization;
using Server.Json;

namespace Server.Engines.Spawners;

[SerializationGenerator(1)]
public partial class Spawner : BaseSpawner
{
    [SerializableField(0)]
    private List<SpawnerEntry> _spawnEntries;

    public override IReadOnlyList<ISpawnerEntry> Entries => _spawnEntries;

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
        int amount, int minDelay, int maxDelay, int team, int homeRange,
        params ReadOnlySpan<string> spawnedNames
    ) : this(
        amount,
        TimeSpan.FromMinutes(minDelay),
        TimeSpan.FromMinutes(maxDelay),
        team,
        homeRange,
        spawnedNames
    )
    {
    }

    [Constructible(AccessLevel.Developer)]
    public Spawner(
        int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange,
        params ReadOnlySpan<string> spawnedNames
    ) : base(amount, minDelay, maxDelay, team, homeRange, spawnedNames)
    {
    }

    public Spawner(DynamicJson json, JsonSerializerOptions options) : base(json, options)
    {
    }

    protected override void InitializeEntries()
    {
        _spawnEntries = [];
    }

    public override ISpawnerEntry AddEntry(
        string creaturename,
        int probability = 100,
        int amount = 1,
        bool dotimer = true,
        string properties = null,
        string parameters = null
    )
    {
        var entry = new SpawnerEntry(this, creaturename, probability, amount, properties, parameters);
        AddToSpawnEntries(entry);

        if (dotimer)
        {
            DoTimer(TimeSpan.FromSeconds(1));
        }

        return entry;
    }

    public override void RemoveEntry(ISpawnerEntry entry)
    {
        if (entry is SpawnerEntry spawnerEntry && _spawnEntries.Contains(spawnerEntry))
        {
            CleanupEntrySpawns(entry);
            RemoveFromSpawnEntries(spawnerEntry);
        }
    }

    public override void ClearAllEntries()
    {
        for (var i = _spawnEntries.Count - 1; i >= 0; i--)
        {
            RemoveEntry(_spawnEntries[i]);
        }
    }

    protected override void OnLegacyEntriesLoaded(List<SpawnerEntry> legacyEntries)
    {
        _spawnEntries = legacyEntries;
    }

    private void MigrateFrom(V0Content content)
    {
        // Version 0 -> 1: Entries moved from BaseSpawner to Spawner
        // Legacy entries are populated via OnLegacyEntriesLoaded called from BaseSpawner.Deserialize
    }

    public static bool IsValidWater(Map map, int x, int y, int z)
    {
        if (!Region.Find(new Point3D(x, y, z), map).AllowSpawn() || !map.CanFit(x, y, z, 16, false, true, false))
        {
            return false;
        }

        var landTile = map.Tiles.GetLandTile(x, y);

        if (landTile.Z == z && (TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags & TileFlag.Wet) != 0)
        {
            return true;
        }

        foreach (var staticTile in map.Tiles.GetStaticAndMultiTiles(x, y))
        {
            if (staticTile.Z == z && TileData.ItemTable[staticTile.ID & TileData.MaxItemValue].Wet)
            {
                return true;
            }
        }

        return false;
    }

    /*
    public override bool OnDefragSpawn(ISpawnable spawned, bool remove)
    {
      // To despawn a mob that was lured 4x away from its spawner
      // TODO: Move this to a config
      if (spawned is BaseCreature c && c.Combatant == null && c.GetDistanceToSqrt( Location ) > c.RangeHome * 4)
      {
        c.Delete();
        remove = true;
      }

      return base.OnDefragSpawn(entry, spawned, remove);
    }
    */

    public override Point3D GetSpawnPosition(ISpawnable spawned, Map map)
    {
        if (map == null || map == Map.Internal)
        {
            return Location;
        }

        bool waterMob, waterOnlyMob;

        if (spawned is Mobile mob)
        {
            waterMob = mob.CanSwim;
            waterOnlyMob = mob.CanSwim && mob.CantWalk;
        }
        else
        {
            waterMob = false;
            waterOnlyMob = false;
        }

        // Try 10 times to find a valid location.
        for (var i = 0; i < 10; i++)
        {
            var x = Location.X + (Utility.Random(HomeRange * 2 + 1) - HomeRange);
            var y = Location.Y + (Utility.Random(HomeRange * 2 + 1) - HomeRange);

            var mapZ = map.GetAverageZ(x, y);

            if (waterMob)
            {
                if (IsValidWater(map, x, y, Z))
                {
                    return new Point3D(x, y, Z);
                }

                if (IsValidWater(map, x, y, mapZ))
                {
                    return new Point3D(x, y, mapZ);
                }
            }

            if (!waterOnlyMob)
            {
                if (map.CanSpawnMobile(x, y, Z))
                {
                    return new Point3D(x, y, Z);
                }

                if (map.CanSpawnMobile(x, y, mapZ))
                {
                    return new Point3D(x, y, mapZ);
                }
            }
        }

        return HomeLocation;
    }
}
