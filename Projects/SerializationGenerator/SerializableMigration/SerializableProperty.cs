/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableProperty.cs                                         *
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

namespace SerializableMigration;

public record SerializableProperty
{
    [JsonPropertyName("name")]
    public string Name { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; }

    [JsonPropertyName("usesSaveFlag")]
    public bool? UsesSaveFlag { get; init; }

    [JsonPropertyName("rule")]
    public string Rule { get; init; }

    [JsonPropertyName("ruleArguments")]
    public string[]? RuleArguments { get; init; }

    [JsonIgnore]
    public int Order { get; init; }
}