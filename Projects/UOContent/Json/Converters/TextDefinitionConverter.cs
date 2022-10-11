/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TextDefinitionConverter.cs                                      *
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
    public class TextDefinitionConverter : JsonConverter<TextDefinition>
    {
        public override TextDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.String => new TextDefinition(reader.GetString()),
                JsonTokenType.Number => new TextDefinition(reader.GetInt32()),
                _                    => throw new JsonException("TextDefinition value must be an integer or string")
            };

        public override void Write(Utf8JsonWriter writer, TextDefinition value, JsonSerializerOptions options)
        {
            if (value.Number > 0)
            {
                writer.WriteNumberValue(value.Number);
            }
            else
            {
                writer.WriteStringValue(value.String);
            }
        }
    }
}
