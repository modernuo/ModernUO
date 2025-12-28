/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
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
using System.Collections.Generic;
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

    public override Region Region => _spawnRegion;

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
        int amount,
        TimeSpan minDelay,
        TimeSpan maxDelay,
        int team = 0,
        params ReadOnlySpan<string> spawnedNames
    ) : base(amount, minDelay, maxDelay, team, spawnedNames: spawnedNames)
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

    // RegionSpawner does not support spiral scan (disjoint rectangles make it ineffective)
    protected override bool SupportsSpiralScan => false;

    protected override Rectangle3D GetBoundsForSpawnAttempt()
    {
        if (_spawnRegion == null || _spawnRegion.TotalWeight <= 0)
        {
            return default;
        }

        // Pick a weighted random rectangle from the region
        var rand = Utility.Random(_spawnRegion.TotalWeight);

        for (var j = 0; j < _spawnRegion.RectangleWeights.Length; j++)
        {
            var curWeight = _spawnRegion.RectangleWeights[j];

            if (rand < curWeight)
            {
                return _spawnRegion.Rectangles[j];
            }

            rand -= curWeight;
        }

        return default;
    }

    protected override IReadOnlyList<Rectangle3D> GetAllSpawnBounds() =>
        _spawnRegion?.Rectangles ?? Array.Empty<Rectangle3D>();

    public override Point3D GetSpawnPosition(ISpawnable spawned, Map map)
    {
        // Check for region/map mismatch before delegating to base
        if (_spawnRegion == null || map == null || map == Map.Internal ||
            map != _spawnRegion.Map || _spawnRegion.TotalWeight <= 0)
        {
            return Location;
        }

        return base.GetSpawnPosition(spawned, map);
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

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        _spawnRegion = Region.Find(_spawnRegionName, Map) as BaseRegion;
        _spawnRegion?.InitRectangles();
    }
}
