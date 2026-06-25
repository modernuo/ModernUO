/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ProximitySpawner.Json.cs                                        *
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

public partial class ProximitySpawner
{
    private int _jsonTriggerRange;
    private TextDefinition _jsonSpawnMessage;
    private bool _jsonInstant;

    [JsonInclude]
    [JsonPropertyName("triggerRange")]
    public int JsonTriggerRange
    {
        get => TriggerRange;
        set => _jsonTriggerRange = value;
    }

    [JsonInclude]
    [JsonPropertyName("spawnMessage")]
    public TextDefinition JsonSpawnMessage
    {
        get => SpawnMessage;
        set => _jsonSpawnMessage = value;
    }

    [JsonInclude]
    [JsonPropertyName("instant")]
    public bool JsonInstant
    {
        get => InstantFlag;
        set => _jsonInstant = value;
    }

    protected internal override void OnAfterJsonDeserialize()
    {
        base.OnAfterJsonDeserialize();

        TriggerRange = _jsonTriggerRange;
        SpawnMessage = _jsonSpawnMessage;
        InstantFlag = _jsonInstant;
    }
}
