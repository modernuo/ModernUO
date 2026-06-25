/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BaseSpawner.Json.cs                                             *
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

namespace Server.Engines.Spawners;

public abstract partial class BaseSpawner
{
    // Transient import-only state (NOT [SerializableField]; never part of the binary save).
    private Guid _jsonGuid;
    private bool _jsonHasGuid;
    private int _jsonCount;
    private TimeSpan _jsonMinDelay = DefaultMinDelay;
    private TimeSpan _jsonMaxDelay = DefaultMaxDelay;
    private int _jsonTeam;
    private int _jsonWalkingRange = -1;
    private int _jsonHomeRange = -1;
    private bool _jsonSpawnLocationIsHome;
    private SpawnPositionMode _jsonSpawnPositionMode;
    private int _jsonMaxSpawnAttempts = DefaultMaxSpawnAttempts;
    private List<SpawnerEntry> _jsonEntries;
    private protected Point3D _jsonLocation;
    private protected Map _jsonMap;

    // --- Always-written fields ---

    [JsonInclude]
    [JsonPropertyName("guid")]
    public Guid JsonGuid
    {
        get => _guid;
        set
        {
            _jsonGuid = value;
            _jsonHasGuid = true;
        }
    }

    [JsonInclude]
    [JsonPropertyName("location")]
    public Point3D JsonLocation
    {
        get => Location;
        set => _jsonLocation = value;
    }

    [JsonInclude]
    [JsonPropertyName("map")]
    public Map JsonMap
    {
        get => Map;
        set => _jsonMap = value;
    }

    [JsonInclude]
    [JsonPropertyName("count")]
    public int JsonCount
    {
        get => Count;
        set => _jsonCount = value;
    }

    [JsonInclude]
    [JsonPropertyName("entries")]
    public List<SpawnerEntry> JsonEntries
    {
        get => Entries;
        set => _jsonEntries = value;
    }

    // --- Conditionally-written fields (null getter => omitted under WhenWritingNull) ---

    [JsonInclude]
    [JsonPropertyName("name")]
    public string JsonName
    {
        get => string.IsNullOrEmpty(Name) ? null : Name;
        set => Name = value;
    }

    [JsonInclude]
    [JsonPropertyName("minDelay")]
    public TimeSpan? JsonMinDelay
    {
        get => _minDelay == DefaultMinDelay ? null : _minDelay;
        set => _jsonMinDelay = value ?? DefaultMinDelay;
    }

    [JsonInclude]
    [JsonPropertyName("maxDelay")]
    public TimeSpan? JsonMaxDelay
    {
        get => _maxDelay == DefaultMaxDelay ? null : _maxDelay;
        set => _jsonMaxDelay = value ?? DefaultMaxDelay;
    }

    [JsonInclude]
    [JsonPropertyName("team")]
    public int? JsonTeam
    {
        get => _team == 0 ? null : _team;
        set => _jsonTeam = value ?? 0;
    }

    // Mirrors today's ToJson exactly: written when _walkingRange != 0, emitting the WalkingRange property.
    [JsonInclude]
    [JsonPropertyName("walkingRange")]
    public int? JsonWalkingRange
    {
        get => _walkingRange != 0 ? WalkingRange : null;
        set => _jsonWalkingRange = value ?? -1;
    }

    [JsonInclude]
    [JsonPropertyName("spawnLocationIsHome")]
    public bool? JsonSpawnLocationIsHome
    {
        get => _spawnLocationIsHome ? true : null;
        set => _jsonSpawnLocationIsHome = value ?? false;
    }

    [JsonInclude]
    [JsonPropertyName("spawnPositionMode")]
    public SpawnPositionMode? JsonSpawnPositionMode
    {
        get => _spawnPositionMode is not SpawnPositionMode.Automatic and not SpawnPositionMode.Abandoned
            ? _spawnPositionMode
            : null;
        set => _jsonSpawnPositionMode = value ?? SpawnPositionMode.Automatic;
    }

    [JsonInclude]
    [JsonPropertyName("maxSpawnAttempts")]
    public int? JsonMaxSpawnAttempts
    {
        // _maxSpawnAttempts == 0 means "unset; use the default". GetSpawnPosition treats
        // both 0 and DefaultMaxSpawnAttempts identically, so omit both from JSON output.
        get => _maxSpawnAttempts > 0 && _maxSpawnAttempts != DefaultMaxSpawnAttempts ? _maxSpawnAttempts : null;
        set => _jsonMaxSpawnAttempts = value ?? DefaultMaxSpawnAttempts;
    }

    // Legacy read-only: present in old files; converted in OnAfterJsonDeserialize. Never written
    // (getter always null) — modern files carry spawnBounds instead.
    [JsonInclude]
    [JsonPropertyName("homeRange")]
    public int? JsonHomeRange
    {
        get => null;
        set => _jsonHomeRange = value ?? -1;
    }

    /// <summary>
    /// Applies the deserialized JSON state to this live spawner. Fired by the resolver's
    /// OnDeserialized after all shadow properties are set. Overrides MUST call base first.
    /// This replaces the former (DynamicJson, options) constructor body.
    /// </summary>
    protected internal virtual void OnAfterJsonDeserialize()
    {
        if (_jsonHasGuid)
        {
            _guid = _jsonGuid;
        }

        // Legacy homeRange -> spawnBounds (Map not available yet; use the deserialized location).
        if (_jsonHomeRange >= 0)
        {
            int z;
            int depth;
            if (_jsonHomeRange == 0)
            {
                z = _jsonLocation.Z;
                depth = 0;
            }
            else
            {
                z = -128;
                depth = 256;
            }

            SpawnBounds = new Rectangle3D(
                _jsonLocation.X - _jsonHomeRange,
                _jsonLocation.Y - _jsonHomeRange,
                z,
                _jsonHomeRange * 2 + 1,
                _jsonHomeRange * 2 + 1,
                depth
            );
        }

        InitSpawn(_jsonCount, _jsonMinDelay, _jsonMaxDelay, _jsonTeam, SpawnBounds);

        _walkingRange = _jsonWalkingRange;
        _spawnLocationIsHome = _jsonSpawnLocationIsHome;
        _spawnPositionMode = _jsonSpawnPositionMode;
        _maxSpawnAttempts = _jsonMaxSpawnAttempts;

        if (_jsonEntries != null)
        {
            for (var i = 0; i < _jsonEntries.Count; i++)
            {
                var entry = _jsonEntries[i];
                AddEntry(entry.SpawnedName, entry.SpawnedProbability, entry.SpawnedMaxCount, false, entry.Properties, entry.Parameters);
            }
        }
    }
}
