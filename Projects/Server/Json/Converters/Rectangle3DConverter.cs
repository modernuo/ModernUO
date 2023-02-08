/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Rectangle3DConverter.cs                                         *
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

public class Rectangle3DConverter : JsonConverter<Rectangle3D>
{
    private Rectangle3D DeserializeArray(ref Utf8JsonReader reader)
    {
        Span<int> data = stackalloc int[6];
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
                if (count < 6)
                {
                    data[count] = reader.GetInt32();
                }

                count++;
            }
        }

        if (count > 6)
        {
            throw new JsonException("Rectangle3D must be an array of x, y, z, h, w, d");
        }

        return new Rectangle3D(data[0], data[1], data[2], data[3], data[4], data[5]);
    }

    private Rectangle3D DeserializeObj(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        Span<int> data = stackalloc int[6];

        // 0 - xyzwhd, 1 - x1y1z1x2y2z2, 2 - start/end
        var objType = -1;
        var hasZ = false;

        while (true)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Invalid json structure for Rectangle3D object");
            }

            var key = reader.GetString();

            reader.Read();

            if (key is "start" or "end")
            {
                if (objType > -1 && objType != 2)
                {
                    throw new JsonException("Rectangle3D must have a start/end, or x/y/z/w/h/d, but not both.");
                }

                objType = 2;

                var point3D = reader.ToObject<Point3D>(options);
                var offset = key == "end" ? 3 : 0;
                data[0 + offset] = point3D.X;
                data[1 + offset] = point3D.Y;
                // We can't do implicit z-level. Abandon using Point3D deserialization?
                data[2 + offset] = point3D.Z;
                continue;
            }

            var i = key switch
            {
                "x"      => 0,
                "y"      => 1,
                "z"      => 2,
                "w"      => 3,
                "width"  => 3,
                "h"      => 4,
                "height" => 4,
                "d"      => 5,
                "depth"  => 5,
                "x1"     => 10,
                "y1"     => 11,
                "z1"     => 12,
                "x2"     => 13,
                "y2"     => 14,
                "z2"     => 15,
                _        => throw new JsonException($"Invalid property {key} for Rectangle3D")
            };

            if (i < 10)
            {
                if (objType > -1 && objType != 0)
                {
                    throw new JsonException("Rectangle3D must have a start/end, or x/y/z/w/h/d, but not both.");
                }

                objType = 0;
                data[i] = reader.GetInt32();
                if (i == 2)
                {
                    hasZ = true;
                }

                continue;
            }

            if (objType > -1 && objType != 1)
            {
                throw new JsonException("Rectangle3D must have a start/end, or x/y/z/w/h/d, but not both.");
            }

            objType = 1;
            data[i - 10] = reader.GetInt32();
            if (i is 12 or 15)
            {
                hasZ = true;
            }
        }

        if (!hasZ)
        {
            // Bottom to top?
            data[2] = -128;
            data[5] = 127;
        }

        return objType == 0
            ? new Rectangle3D(data[0], data[1], data[2], data[3], data[4], data[5])
            : new Rectangle3D(
                new Point3D(data[0], data[1], data[2]),
                new Point3D(data[3], data[4], data[5])
            );
    }

    public override Rectangle3D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String      => Rectangle3D.Parse(reader.GetString(), null),
            JsonTokenType.StartArray  => DeserializeArray(ref reader),
            JsonTokenType.StartObject => DeserializeObj(ref reader, options),
            _                         => throw new JsonException("Invalid Json for Point3D")
        };

    public override void Write(Utf8JsonWriter writer, Rectangle3D value, JsonSerializerOptions options)
    {
        var writeZ = value.Start.Z is > sbyte.MinValue and < sbyte.MaxValue || value.End.Z is > sbyte.MinValue and < sbyte.MaxValue;

        writer.WriteStartObject();
        writer.WritePropertyName("x1");
        writer.WriteNumberValue(value.Start.X);
        writer.WritePropertyName("y1");
        writer.WriteNumberValue(value.Start.Y);

        if (writeZ)
        {
            writer.WritePropertyName("z1");
            writer.WriteNumberValue(value.Start.Z);
        }

        writer.WritePropertyName("x2");
        writer.WriteNumberValue(value.End.X);
        writer.WritePropertyName("y2");
        writer.WriteNumberValue(value.End.Y);
        if (writeZ)
        {
            writer.WritePropertyName("z2");
            writer.WriteNumberValue(value.End.Z);
        }
        writer.WriteEndObject();
    }
}
