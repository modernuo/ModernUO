/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DynamicJson.cs                                                  *
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
using System.Text.Json.Serialization;

namespace Server.Json;

public class DynamicJson
{
    public static DynamicJson Create(Type type) => new()
    {
        Type = type.Name,
        Data = new Dictionary<string, JsonElement>()
    };

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Data { get; set; }

    // TODO: Use JSON Node in .NET 6
    public void SetProperty<T>(string key, JsonSerializerOptions options, T value)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(value, options));
        Data[key] = doc.RootElement.Clone();
    }

    public bool GetProperty<T>(string key, JsonSerializerOptions options, out T t)
    {
        if (Data.TryGetValue(key, out var el))
        {
            t = el.ToObject<T>(options);
            return true;
        }

        t = default;
        return false;
    }

    public bool GetEnumProperty<T>(string key, JsonSerializerOptions options, out T t) where T : struct, Enum
    {
        if (Data.TryGetValue(key, out var el))
        {
            return Enum.TryParse(el.ToObject<string>(options), out t);
        }

        t = default;
        return false;
    }
}
