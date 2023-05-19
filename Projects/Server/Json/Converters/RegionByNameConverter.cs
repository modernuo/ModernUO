/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: RegionByNameConverter.cs                                        *
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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server.Json;

public class RegionByNameConverter : JsonConverter<Region>
{
    public override Region Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string name = null;
        Map map = null;

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Invalid json for RegionByName");
        }

        while (true)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Invalid json for RegionByName");
            }

            string property = reader.GetString()?.ToLower();

            if (property != "name" && property != "map")
            {
                continue;
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Value for {property} must be a string");
            }

            if (property == "name")
            {
                name = reader.GetString();
            }
            else
            {
                map = Map.Parse(reader.GetString());
            }
        }

        if (name == null || map == null)
        {
            throw new JsonException("Invalid json for RegionByName");
        }

        return Region.Find(name, map);
    }

    public override void Write(Utf8JsonWriter writer, Region value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Name", value.Name);
        writer.WriteString("Map", value.Map.Name);
        writer.WriteEndObject();
    }
}
