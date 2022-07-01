/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TypeConverter.cs                                                *
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
using Server.Logging;

namespace Server.Json;

public class TypeConverter : JsonConverter<Type>
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TypeConverter));

    public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("The JSON value could not be converted to System.Type");
        }

        var typeName = reader.GetString();
        var type = AssemblyHandler.FindTypeByName(typeName);
        if (type == null)
        {
            logger.Warning("Attempted to deserialize type {Type} which does not exist.", typeName);
        }

        return type;
    }

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.FullName);
}
