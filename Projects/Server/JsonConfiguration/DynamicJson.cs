/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: DynamicJson.cs - Created: 2020/05/23 - Updated: 2020/05/23      *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server.Json
{
    public class DynamicJson
    {
        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonExtensionData] public Dictionary<string, JsonElement> data { get; set; }

        public bool GetProperty<T>(string key, JsonSerializerOptions options, out T t)
        {
            if (data.TryGetValue(key, out var el))
            {
                t = el.ToObject<T>(options);
                return true;
            }

            t = default;
            return false;
        }

        public bool GetEnumProperty<T>(string key, JsonSerializerOptions options, out T t) where T : struct, Enum
        {
            if (data.TryGetValue(key, out var el))
                return Enum.TryParse(el.ToObject<string>(options), out t);

            t = default;
            return false;
        }
    }
}
