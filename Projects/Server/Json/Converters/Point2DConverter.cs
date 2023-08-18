/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Point2DConverter.cs                                             *
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

public class Point2DConverter : JsonConverter<Point2D>
{
    private Point2D DeserializeArray(ref Utf8JsonReader reader)
    {
        Span<int> data = stackalloc int[2];
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
                if (count < 2)
                {
                    data[count] = reader.GetInt32();
                }

                count++;
            }
        }

        if (count > 2)
        {
            throw new JsonException("Point2D must be an array of x, y");
        }

        return new Point2D(data[0], data[1]);
    }

    private Point2D DeserializeObj(ref Utf8JsonReader reader)
    {
        Span<int> data = stackalloc int[2];

        while (true)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Invalid json structure for Point2D object");
            }

            var key = reader.GetString();

            var i = key switch
            {
                "x" => 0,
                "y" => 1,
                _   => throw new JsonException($"Invalid property {key} for Point2D")
            };

            reader.Read();

            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException($"Value for {key} must be a number");
            }

            data[i] = reader.GetInt32();
        }

        return new Point2D(data[0], data[1]);
    }

    public override Point2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => Point2D.Parse(reader.GetString(), null),
            JsonTokenType.StartArray  => DeserializeArray(ref reader),
            JsonTokenType.StartObject => DeserializeObj(ref reader),
            _                         => throw new JsonException("Invalid json for Point3D")
        };

    public override void Write(Utf8JsonWriter writer, Point2D value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }
}
