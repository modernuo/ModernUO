/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Point3DConverter.cs                                             *
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

public class Point3DConverter : JsonConverter<Point3D>
{
    private Point3D DeserializeArray(ref Utf8JsonReader reader)
    {
        Span<int> data = stackalloc int[3];
        var count = 0;

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

                count++;
            }
        }

        if (count > 3)
        {
            throw new JsonException("Point3D must be an array of x, y, z");
        }

        return new Point3D(data[0], data[1], data[2]);
    }

    private Point3D DeserializeObj(ref Utf8JsonReader reader)
    {
        Span<int> data = stackalloc int[3];

        while (true)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Invalid json structure for Point3D object");
            }

            var key = reader.GetString();

            var i = key switch
            {
                "x" => 0,
                "y" => 1,
                "z" => 2,
                _   => throw new JsonException($"Invalid property {key} for Point3D")
            };

            reader.Read();

            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException($"Value for {key} must be a number");
            }

            data[i] = reader.GetInt32();
        }

        return new Point3D(data[0], data[1], data[2]);
    }

    public override Point3D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String      => Point3D.Parse(reader.GetString(), null),
            JsonTokenType.StartArray  => DeserializeArray(ref reader),
            JsonTokenType.StartObject => DeserializeObj(ref reader),
            _                         => throw new JsonException("Invalid Json for Point3D")
        };

    public override void Write(Utf8JsonWriter writer, Point3D value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteNumberValue(value.X);
        writer.WritePropertyName("y");
        writer.WriteNumberValue(value.Y);
        writer.WritePropertyName("z");
        writer.WriteNumberValue(value.Z);
        writer.WriteEndObject();
    }
}
