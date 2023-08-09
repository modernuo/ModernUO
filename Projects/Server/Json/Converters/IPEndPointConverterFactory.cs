/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: IPEndPointConverterFactory.cs                                   *
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

public class IPEndPointConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(IPEndPoint);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        new IPEndPointConverter();
}
