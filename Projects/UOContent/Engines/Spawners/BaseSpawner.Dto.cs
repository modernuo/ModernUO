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
        _guid = dto.Guid == Guid.Empty ? Guid.NewGuid() : dto.Guid;

        if (!string.IsNullOrEmpty(dto.Name))
        {
            Name = dto.Name;
        }

        // Compact homeRange -> spawnBounds (Map not available yet; use the DTO location).
        if (dto.HomeRange >= 0)
        {
            SpawnBounds = BoundsFromHomeRange(dto.Location, dto.HomeRange);
        }

        InitSpawn(dto.Count, dto.MinDelay, dto.MaxDelay, dto.Team, SpawnBounds);

        _walkingRange = dto.WalkingRange;
        _spawnLocationIsHome = dto.SpawnLocationIsHome;
        _spawnPositionMode = dto.SpawnPositionMode;
        _maxSpawnAttempts = dto.MaxSpawnAttempts;

        if (dto.Entries != null)
        {
            for (var i = 0; i < dto.Entries.Count; i++)
            {
                var entry = dto.Entries[i];
                AddEntry(entry.SpawnedName, entry.SpawnedProbability, entry.SpawnedMaxCount, false, entry.Properties, entry.Parameters);
            }
        }
    }

    /// <summary>The square spawn bounds a homeRange radius represents (centered on the location).</summary>
    private protected static Rectangle3D BoundsFromHomeRange(Point3D location, int homeRange)
    {
        int z;
        int depth;
        if (homeRange == 0)
        {
            z = location.Z;
            depth = 0;
        }
        else
        {
            z = -128;
            depth = 256;
        }

        return new Rectangle3D(
            location.X - homeRange,
            location.Y - homeRange,
            z,
            homeRange * 2 + 1,
            homeRange * 2 + 1,
            depth
        );
    }

    // Export helpers. Options use WhenWritingDefault, so optional fields are omitted at their CLR
    // default; helpers below map non-CLR-default "omit" values onto the default so they drop out.
    private protected Guid DtoGuid => _guid;
    private protected string DtoName => string.IsNullOrEmpty(Name) ? null : Name;
    private protected TimeSpan DtoMinDelay => _minDelay;
    private protected TimeSpan DtoMaxDelay => _maxDelay;
    private protected int DtoTeam => _team;
    private protected int DtoWalkingRange => _walkingRange;
    private protected bool DtoSpawnLocationIsHome => _spawnLocationIsHome;

    // Abandoned is a transient "gave up" runtime state, not persisted -> map to Automatic (omitted).
    private protected SpawnPositionMode DtoSpawnPositionMode =>
        _spawnPositionMode == SpawnPositionMode.Abandoned ? SpawnPositionMode.Automatic : _spawnPositionMode;

    // Runtime treats 0 identically to DefaultMaxSpawnAttempts(10) -> map the default to 0 (omitted).
    private protected int DtoMaxSpawnAttempts =>
        _maxSpawnAttempts == DefaultMaxSpawnAttempts ? 0 : _maxSpawnAttempts;

    // The homeRange radius if SpawnBounds is EXACTLY what that radius reconstructs (square, centered,
    // standard z/depth) so the round-trip is lossless; otherwise -1 (write spawnBounds instead).
    private protected int DtoHomeRange
    {
        get
        {
            if (SpawnBounds == default || !IsHomeRangeStyle)
            {
                return -1;
            }

            var homeRange = HomeRange;
            return SpawnBounds == BoundsFromHomeRange(Location, homeRange) ? homeRange : -1;
        }
    }
}
