/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IPEndPointConverter.cs                                          *
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
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server.Json;

public class IPEndPointConverter : JsonConverter<IPEndPoint>
{
    public override IPEndPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (IPEndPoint.TryParse(reader.GetString()!, out var ipep))
        {
            return ipep;
        }

        throw new JsonException("IPEndPoint must be in the correct format");
    }

    public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
