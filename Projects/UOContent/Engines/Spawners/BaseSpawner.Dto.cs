/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BaseSpawner.Dto.cs                                              *
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

namespace Server.Engines.Spawners;

public abstract partial class BaseSpawner
{
    /// <summary>Applies the common DTO fields to this freshly-created spawner (import path).</summary>
    internal void ApplyDto(SpawnerDto dto)
    {
        _guid = dto.Guid ?? Guid.NewGuid();

        if (!string.IsNullOrEmpty(dto.Name))
        {
            Name = dto.Name;
        }

        // Legacy homeRange -> spawnBounds (Map not available yet; use the DTO location).
        if (dto.HomeRange is int homeRange && homeRange >= 0)
        {
            int z;
            int depth;
            if (homeRange == 0)
            {
                z = dto.Location.Z;
                depth = 0;
            }
            else
            {
                z = -128;
                depth = 256;
            }

            SpawnBounds = new Rectangle3D(
                dto.Location.X - homeRange,
                dto.Location.Y - homeRange,
                z,
                homeRange * 2 + 1,
                homeRange * 2 + 1,
                depth
            );
        }

        InitSpawn(dto.Count, dto.MinDelay ?? DefaultMinDelay, dto.MaxDelay ?? DefaultMaxDelay, dto.Team ?? 0, SpawnBounds);

        _walkingRange = dto.WalkingRange ?? -1;
        _spawnLocationIsHome = dto.SpawnLocationIsHome ?? false;
        _spawnPositionMode = dto.SpawnPositionMode ?? SpawnPositionMode.Automatic;
        _maxSpawnAttempts = dto.MaxSpawnAttempts ?? DefaultMaxSpawnAttempts;

        if (dto.Entries != null)
        {
            for (var i = 0; i < dto.Entries.Count; i++)
            {
                var entry = dto.Entries[i];
                AddEntry(entry.SpawnedName, entry.SpawnedProbability, entry.SpawnedMaxCount, false, entry.Properties, entry.Parameters);
            }
        }
    }

    // Export helpers — nullable so WhenWritingNull omits domain defaults, matching legacy ToJson.
    private protected Guid? DtoGuid => _guid;
    private protected string DtoName => string.IsNullOrEmpty(Name) ? null : Name;
    private protected TimeSpan? DtoMinDelay => _minDelay == DefaultMinDelay ? null : _minDelay;
    private protected TimeSpan? DtoMaxDelay => _maxDelay == DefaultMaxDelay ? null : _maxDelay;
    private protected int? DtoTeam => _team == 0 ? null : _team;
    private protected int? DtoWalkingRange => _walkingRange != 0 ? WalkingRange : null;
    private protected bool? DtoSpawnLocationIsHome => _spawnLocationIsHome ? true : null;

    private protected SpawnPositionMode? DtoSpawnPositionMode =>
        _spawnPositionMode is not SpawnPositionMode.Automatic and not SpawnPositionMode.Abandoned
            ? _spawnPositionMode
            : null;

    // Runtime treats 0 identically to DefaultMaxSpawnAttempts (BaseSpawner.cs maxAttempts clamp),
    // so both are omitted — fresh spawners have _maxSpawnAttempts == 0.
    private protected int? DtoMaxSpawnAttempts =>
        _maxSpawnAttempts > 0 && _maxSpawnAttempts != DefaultMaxSpawnAttempts ? _maxSpawnAttempts : null;
}
