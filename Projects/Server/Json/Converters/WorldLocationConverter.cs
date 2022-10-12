/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: WorldLocationConverter.cs                                       *
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

public class WorldLocationConverter : JsonConverter<WorldLocation>
{
    private static Point3DConverter _point3DConverter;
    private static MapConverter _mapConverter;

    private WorldLocation DeserializeArray(ref Utf8JsonReader reader)
    {
        Span<int> data = stackalloc int[3];
        var count = 0;
        var hasMap = false;
        Map map = null;

        while (true)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (count < 3)
                {
                    data[count] = reader.GetInt32();
                }
                else if (count == 3)
                {
                    map = Map.Maps[reader.GetInt32()];
                    hasMap = true;
                }

                count++;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var key = reader.GetString();

                if (count != 3 || hasMap)
                {
                    throw new JsonException($"Value {key} is not valid for this element.");
                }

                map = Map.Parse(key);
                hasMap = true;
                break;
            }
        }

        if (!hasMap || count != 3)
        {
            throw new JsonException("WorldLocation must be an array of x, y, z, and map");
        }

        return new WorldLocation(data[0], data[1], data[2], map);
    }

    private WorldLocation DeserializeObj(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        Span<int> data = stackalloc int[3];
        var count = 0;
        var hasLoc = false;
        var hasXYZ = false;
        var hasMap = false;
        Map map = null;

        while (true)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Invalid Json structure for WorldLocation object");
            }

            var key = reader.GetString();

            var i = key switch
            {
                "x"   => 0,
                "y"   => 1,
                "z"   => 2,
                "loc" => 3,
                "map" => 4,
                _     => 5
            };

            if (i == 5)
            {
                continue;
            }

            reader.Read();

            if (i < 3)
            {
                if (hasLoc)
                {
                    throw new JsonException("WorldLocation must have loc or x, y, z, but not both");
                }

                if (reader.TokenType != JsonTokenType.Number)
                {
                    throw new JsonException($"Value for {key} must be a number");
                }

                hasXYZ = true;
                data[i] = reader.GetInt32();
                continue;
            }

            if (i == 3)
            {
                if (hasXYZ)
                {
                    throw new JsonException("WorldLocation must have loc or x, y, z, but not both");
                }

                hasLoc = true;

                _point3DConverter ??= new Point3DConverter();

                var loc = _point3DConverter.Read(ref reader, typeof(Point3D), options);
                data[0] = loc.X;
                data[1] = loc.Y;
                data[2] = loc.Z;
                count = 3;
                continue;
            }

            _mapConverter ??= new MapConverter();
            map = _mapConverter.Read(ref reader, typeof(Map), options);

            hasMap = true;
        }

        if (!hasMap || count != 3)
        {
            throw new JsonException("WorldLocation must have an x, y, z, and map properties");
        }

        return new WorldLocation(data[0], data[1], data[2], map);
    }

    public override WorldLocation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.StartArray  => DeserializeArray(ref reader),
            JsonTokenType.StartObject => DeserializeObj(ref reader, options),
            _                         => throw new JsonException("Invalid Json for Point3D")
        };

    public override void Write(Utf8JsonWriter writer, WorldLocation value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteStringValue(value.Map.ToString());
        writer.WriteEndArray();
    }
}
