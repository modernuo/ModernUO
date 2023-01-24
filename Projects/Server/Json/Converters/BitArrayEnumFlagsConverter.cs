/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BitArrayEnumFlagsConverter.cs                                   *
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
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Server.Collections;

namespace Server.Json;

static file class BitArrayEnumFlagsConverterExtensions
{
    internal static readonly Dictionary<Type, int> _maxValueForEnum = new();
}

public class BitArrayEnumFlagsConverter<T> : JsonConverter<BitArray> where T : struct, Enum
{
    public override BitArray Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var underlyingType = Enum.GetUnderlyingType(typeof(T));
        var maxValues = BitArrayEnumFlagsConverterExtensions._maxValueForEnum;

        ref var maxValue = ref CollectionsMarshal.GetValueRefOrAddDefault(maxValues, underlyingType, out var exists);
        if (!exists)
        {
            // Get the max value and cache it.
            var values = Enum.GetValues<T>();
            for (var i = 0; i < values.Length; i++)
            {
                var v = (int)(object)values[i];
                if (v > maxValue)
                {
                    maxValue = v;
                }
            }
        }

        // If the value is very large, we will have a major problem.
        // We always assume zero-offset
        var bitArray = new BitArray(maxValue + 1);

        while (true)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Invalid Json structure for Flag object");
            }

            var key = reader.GetString();

            reader.Read();

            if (!reader.GetBoolean() || !Enum.TryParse<T>(key, out var val))
            {
                continue;
            }

            bitArray[(int)(object)val] = true;
        }

        return bitArray;
    }

    public override void Write(Utf8JsonWriter writer, BitArray bitArray, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var flagName in Enum.GetNames(typeof(T)))
        {
            var value = (int)(object)Enum.Parse<T>(flagName, false);
            writer.WriteBoolean(flagName, bitArray[value]);
        }

        writer.WriteEndObject();
    }
}
