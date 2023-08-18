/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ClientVersionConverter.cs                                       *
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

public class ClientVersionConverter : JsonConverter<ClientVersion>
{
    public override ClientVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new ClientVersion(reader.GetString());
        }

        throw new JsonException("Value must be a string");
    }

    public override void Write(Utf8JsonWriter writer, ClientVersion value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
