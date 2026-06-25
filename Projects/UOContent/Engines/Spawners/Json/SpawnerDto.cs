/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SpawnerDto.cs                                                   *
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
using System.Text.Json.Serialization;
using Server.Json;
using Server.Regions;

namespace Server.Engines.Spawners;

/// <summary>
/// Plain data carrier for spawner JSON. System.Text.Json deserializes into these records
/// (never a live Item), so a malformed file fails as GC only. <see cref="ToSpawner"/> builds
/// the real spawner from a fully-validated DTO. Sparse fields are nullable so WhenWritingNull
/// omits domain defaults, matching the legacy ToJson output.
/// </summary>
public abstract record SpawnerDto
{
    [JsonPropertyName("guid")] public Guid? Guid { get; init; }
    [JsonPropertyName("location")] public Point3D Location { get; init; }
    [JsonPropertyName("map")] public Map Map { get; init; }
    [JsonPropertyName("count")] public int Count { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; }
    [JsonPropertyName("minDelay")] public TimeSpan? MinDelay { get; init; }
    [JsonPropertyName("maxDelay")] public TimeSpan? MaxDelay { get; init; }
    [JsonPropertyName("team")] public int? Team { get; init; }
    [JsonPropertyName("walkingRange")] public int? WalkingRange { get; init; }
    [JsonPropertyName("homeRange")] public int? HomeRange { get; init; } // legacy read; never written
    [JsonPropertyName("spawnLocationIsHome")] public bool? SpawnLocationIsHome { get; init; }
    [JsonPropertyName("spawnPositionMode")] public SpawnPositionMode? SpawnPositionMode { get; init; }
    [JsonPropertyName("maxSpawnAttempts")] public int? MaxSpawnAttempts { get; init; }
    [JsonPropertyName("entries")] public List<SpawnerEntry> Entries { get; init; }

    /// <summary>Constructs the empty concrete spawner Item for this DTO.</summary>
    protected abstract BaseSpawner CreateEmpty();

    /// <summary>Builds and populates the live spawner. Override to apply subtype fields after base.</summary>
    public virtual BaseSpawner ToSpawner()
    {
        var spawner = CreateEmpty();
        try
        {
            spawner.ApplyDto(this);
            return spawner;
        }
        catch
        {
            // CreateEmpty already registered the Item in the world; if population throws,
            // delete it here so no orphan can escape (the import catch can't reach it).
            spawner.Delete();
            throw;
        }
    }
}

[JsonDiscoverableType("Spawner")]
public sealed record SpawnerDataDto : SpawnerDto
{
    [JsonPropertyName("spawnBounds")] public Rectangle3D? SpawnBounds { get; init; }

    protected override BaseSpawner CreateEmpty() => new Spawner();

    public override BaseSpawner ToSpawner()
    {
        var spawner = (Spawner)base.ToSpawner();
        try
        {
            if (SpawnBounds is { } bounds && bounds != default)
            {
                spawner.SpawnBounds = bounds;
            }

            return spawner;
        }
        catch
        {
            spawner.Delete();
            throw;
        }
    }
}

[JsonDiscoverableType("RegionSpawner")]
public sealed record RegionSpawnerDto : SpawnerDto
{
    [JsonPropertyName("region")] public string Region { get; init; }

    protected override BaseSpawner CreateEmpty() => new RegionSpawner();

    public override BaseSpawner ToSpawner()
    {
        var spawner = (RegionSpawner)base.ToSpawner();
        try
        {
            spawner.SpawnRegion = Server.Region.Find(Region, Map) as BaseRegion;
            return spawner;
        }
        catch
        {
            spawner.Delete();
            throw;
        }
    }
}

[JsonDiscoverableType("ProximitySpawner")]
public sealed record ProximitySpawnerDto : SpawnerDto
{
    [JsonPropertyName("spawnBounds")] public Rectangle3D? SpawnBounds { get; init; }
    [JsonPropertyName("triggerRange")] public int TriggerRange { get; init; }
    [JsonPropertyName("spawnMessage")] public TextDefinition SpawnMessage { get; init; }
    [JsonPropertyName("instant")] public bool Instant { get; init; }

    protected override BaseSpawner CreateEmpty() => new ProximitySpawner();

    public override BaseSpawner ToSpawner()
    {
        var spawner = (ProximitySpawner)base.ToSpawner();
        try
        {
            if (SpawnBounds is { } bounds && bounds != default)
            {
                spawner.SpawnBounds = bounds;
            }

            spawner.TriggerRange = TriggerRange;
            spawner.SpawnMessage = SpawnMessage;
            spawner.InstantFlag = Instant;
            return spawner;
        }
        catch
        {
            spawner.Delete();
            throw;
        }
    }
}
