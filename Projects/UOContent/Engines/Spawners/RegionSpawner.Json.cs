/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: RegionSpawner.Json.cs                                           *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Text.Json.Serialization;
using Server.Regions;

namespace Server.Engines.Spawners;

public partial class RegionSpawner
{
    private string _jsonRegion;

    [JsonInclude]
    [JsonPropertyName("region")]
    public string JsonRegion
    {
        get => SpawnRegion?.Name;
        set => _jsonRegion = value;
    }

    protected internal override void OnAfterJsonDeserialize()
    {
        base.OnAfterJsonDeserialize();

        _spawnRegion = Region.Find(_jsonRegion, _jsonMap) as BaseRegion;
        _spawnRegion?.InitRectangles();
        SpawnRegionName = _spawnRegion?.Name;
    }
}
