/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TimeSpanConverter.cs                                            *
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

namespace Server.Json
{
    public class FlagsConverter<T> : JsonConverter<T> where T : struct
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var flags = 0x0;

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

                var val = reader.GetBoolean();

                if (val)
                {
                    Enum.TryParse<T>(key, out var flag);
                    flags |= (int) (object) flag;
                }
            }

            return (T) (object) flags;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var flagName in Enum.GetNames(typeof(T)))
            {
                if (!flagName.StartsWith("Expansion"))
                {
                    var flag = Enum.Parse<T>(flagName, false);
                    writer.WriteBoolean(flagName, ((int) (object) value & (int) (object) flag) != 0);
                }
            }
            writer.WriteEndObject();
        }
    }
}
