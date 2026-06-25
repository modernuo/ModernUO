/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Spawner.Json.cs                                                 *
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

namespace Server.Engines.Spawners;

public partial class Spawner
{
    private Rectangle3D _jsonSpawnBounds;

    [JsonInclude]
    [JsonPropertyName("spawnBounds")]
    public Rectangle3D? JsonSpawnBounds
    {
        get => _spawnBounds == default ? null : _spawnBounds;
        set => _jsonSpawnBounds = value ?? default;
    }

    protected internal override void OnAfterJsonDeserialize()
    {
        base.OnAfterJsonDeserialize();

        if (_jsonSpawnBounds != default)
        {
            SpawnBounds = _jsonSpawnBounds;
        }
    }
}
