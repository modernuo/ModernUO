/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: JsonDtoConverter.cs                                             *
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

public class JsonDtoConverter<TDto, TObject> : JsonConverter<TObject>
    where TDto : IJsonRootDtoConvertible<TDto, TObject>, new() where TObject : new()
{
    public override TObject Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (typeof(TObject) != typeToConvert)
        {
            throw new JsonException($"Invalid object type to deserialize for '{typeof(TObject).Name}'");
        }

        // Deserialize to the DTO
        var dto = JsonSerializer.Deserialize<TDto>(ref reader, options);

        return TDto.ToObject(dto);
    }

    public override void Write(Utf8JsonWriter writer, TObject value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, TDto.FromObject(value), options);
    }
}
