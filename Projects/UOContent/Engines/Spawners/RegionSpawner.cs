/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: RegionSpawner.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Text.Json;
using ModernUO.Serialization;
using Server.Json;
using Server.Regions;

namespace Server.Engines.Spawners;

[SerializationGenerator(0)]
public partial class RegionSpawner : Spawner
{
    [SerializableField(0, getter: "private", setter: "private")]
    private string _spawnRegionName;

    private BaseRegion _spawnRegion;

    [Constructible(AccessLevel.Developer)]
    public RegionSpawner()
    {
    }

    [Constructible(AccessLevel.Developer)]
    public RegionSpawner(string spawnedName) : base(spawnedName)
    {
    }

    [Constructible(AccessLevel.Developer)]
    public RegionSpawner(
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
    public RegionSpawner(
        int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange,
        params ReadOnlySpan<string> spawnedNames
    ) : base(amount, minDelay, maxDelay, team, homeRange, spawnedNames)
    {
    }

    public RegionSpawner(DynamicJson json, JsonSerializerOptions options) : base(json, options)
    {
        json.GetProperty("map", options, out Map map);
        json.GetProperty("region", options, out string spawnRegion);

        _spawnRegion = Region.Find(spawnRegion, map) as BaseRegion;
        _spawnRegion?.InitRectangles();
    }

    [CommandProperty(AccessLevel.Developer)]
    public BaseRegion SpawnRegion
    {
        get => _spawnRegion;
        set
        {
            _spawnRegion = value;
            SpawnRegionName = _spawnRegion?.Name;
            _spawnRegion?.InitRectangles();
            InvalidateProperties();
        }
    }

    public override void ToJson(DynamicJson json, JsonSerializerOptions options)
    {
        base.ToJson(json, options);
        json.SetProperty("region", options, SpawnRegion.Name);
    }

    public override void GetSpawnerProperties(IPropertyList list)
    {
        base.GetSpawnerProperties(list);

        if (Running && _spawnRegion != null)
        {
            list.Add(1076228, $"{"region:"}\t{_spawnRegion.Name}"); // ~1_DUMMY~ ~2_DUMMY~
        }
    }

    public override Point3D GetSpawnPosition(ISpawnable spawned, Map map)
    {
        if (_spawnRegion == null || map == null || map == Map.Internal || map != _spawnRegion.Map ||
            _spawnRegion.TotalWeight <= 0)
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
            var rand = Utility.Random(_spawnRegion.TotalWeight);

            var x = int.MinValue;
            var y = int.MinValue;

            for (var j = 0; j < _spawnRegion.RectangleWeights.Length; j++)
            {
                var curWeight = _spawnRegion.RectangleWeights[j];

                if (rand < curWeight)
                {
                    var rect = _spawnRegion.Rectangles[j];

                    x = rect.Start.X + rand % rect.Width;
                    y = rect.Start.Y + rand / rect.Width;

                    break;
                }

                rand -= curWeight;
            }

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

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        _spawnRegion = Region.Find(_spawnRegionName, Map) as BaseRegion;
        _spawnRegion?.InitRectangles();
    }
}
