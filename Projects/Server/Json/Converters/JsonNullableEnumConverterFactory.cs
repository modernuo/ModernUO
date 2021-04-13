/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: JsonNullableEnumConverterFactory.cs                             *
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

namespace Server.Json.Converters
{
    public class JsonNullableEnumConverterFactory : JsonConverterFactory
    {
        readonly JsonStringEnumConverter _stringEnumConverter;

        public JsonNullableEnumConverterFactory(JsonNamingPolicy? namingPolicy = null, bool allowIntegerValues = true) =>
            _stringEnumConverter = new JsonStringEnumConverter(namingPolicy, allowIntegerValues);

        public override bool CanConvert(Type typeToConvert) => Nullable.GetUnderlyingType(typeToConvert)?.IsEnum == true;

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var type = Nullable.GetUnderlyingType(typeToConvert);
            return (JsonConverter?)Activator.CreateInstance(typeof(JsonNullableEnumConverter<>).MakeGenericType(type!),
                _stringEnumConverter.CreateConverter(type, options));
        }
    }
}
