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
/// Plain data carrier for spawner JSON. STJ deserializes into these records (never a live Item),
/// so a malformed file fails as GC only; <see cref="ToSpawner"/> builds the spawner from a
/// validated DTO. Options use <c>WhenWritingDefault</c>; mandatory fields force-write via
/// <c>[JsonIgnore(Condition = Never)]</c> and <c>[JsonPropertyOrder]</c> sets the on-disk order.
/// </summary>
public abstract record SpawnerDto
{
    // Mandatory: always written.
    [JsonPropertyName("guid")]
    [JsonPropertyOrder(0)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public Guid Guid { get; init; }

    [JsonPropertyName("name")]
    [JsonPropertyOrder(1)]
    public string Name { get; init; }

    [JsonPropertyName("location")]
    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [JsonConverter(typeof(Point3DArrayConverter))]
    public Point3D Location { get; init; }

    [JsonPropertyName("map")]
    [JsonPropertyOrder(3)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public Map Map { get; init; }

    [JsonPropertyName("count")]
    [JsonPropertyOrder(4)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public int Count { get; init; }

    [JsonPropertyName("minDelay")]
    [JsonPropertyOrder(5)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public TimeSpan MinDelay { get; init; }

    [JsonPropertyName("maxDelay")]
    [JsonPropertyOrder(6)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public TimeSpan MaxDelay { get; init; }

    // Optional: omitted at default (spawnBounds/region live on the subtypes at order 8).
    [JsonPropertyName("team")]
    [JsonPropertyOrder(7)]
    public int Team { get; init; }

    [JsonPropertyName("walkingRange")]
    [JsonPropertyOrder(9)]
    public int WalkingRange { get; init; }

    [JsonPropertyName("entries")]
    [JsonPropertyOrder(10)]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public List<SpawnerEntry> Entries { get; init; }

    [JsonPropertyName("spawnLocationIsHome")]
    [JsonPropertyOrder(11)]
    public bool SpawnLocationIsHome { get; init; }

    [JsonPropertyName("spawnPositionMode")]
    [JsonPropertyOrder(12)]
    public SpawnPositionMode SpawnPositionMode { get; init; }

    [JsonPropertyName("maxSpawnAttempts")]
    [JsonPropertyOrder(13)]
    public int MaxSpawnAttempts { get; init; }

    // Compact square-bounds form, -1 when absent. Written only when >= 0 (ShouldSerialize, since 0
    // is a valid radius WhenWritingDefault cannot emit); reconstructs SpawnBounds in ApplyDto.
    [JsonPropertyName("homeRange")]
    [JsonPropertyOrder(8)]
    public int HomeRange { get; init; } = -1;

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
            // CreateEmpty registered the Item; delete it if population throws so no orphan escapes.
            spawner.Delete();
            throw;
        }
    }
}

[JsonDiscoverableType("Spawner")]
public sealed record SpawnerDataDto : SpawnerDto
{
    [JsonPropertyName("spawnBounds")]
    [JsonPropertyOrder(8)]
    public Rectangle3D SpawnBounds { get; init; }

    protected override BaseSpawner CreateEmpty() => new Spawner();

    public override BaseSpawner ToSpawner()
    {
        var spawner = (Spawner)base.ToSpawner();
        try
        {
            if (SpawnBounds != default)
            {
                spawner.SpawnBounds = SpawnBounds;
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
    [JsonPropertyName("region")]
    [JsonPropertyOrder(8)]
    public string Region { get; init; }

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
    [JsonPropertyName("spawnBounds")]
    [JsonPropertyOrder(8)]
    public Rectangle3D SpawnBounds { get; init; }

    [JsonPropertyName("triggerRange")]
    [JsonPropertyOrder(14)]
    public int TriggerRange { get; init; }

    [JsonPropertyName("spawnMessage")]
    [JsonPropertyOrder(15)]
    public TextDefinition SpawnMessage { get; init; }

    [JsonPropertyName("instant")]
    [JsonPropertyOrder(16)]
    public bool Instant { get; init; }

    protected override BaseSpawner CreateEmpty() => new ProximitySpawner();

    public override BaseSpawner ToSpawner()
    {
        var spawner = (ProximitySpawner)base.ToSpawner();
        try
        {
            if (SpawnBounds != default)
            {
                spawner.SpawnBounds = SpawnBounds;
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
