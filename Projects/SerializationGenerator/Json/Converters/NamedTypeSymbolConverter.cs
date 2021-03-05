/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: NamedTypeSymbolConverter.cs                                     *
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
using Microsoft.CodeAnalysis;

namespace Server.Json
{
    public class NamedTypeSymbolConverter : JsonConverter<INamedTypeSymbol>
    {
        private Compilation _compilation;
        public NamedTypeSymbolConverter(Compilation compilation) => _compilation = compilation;

        public override INamedTypeSymbol Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("The JSON value could not be converted to Microsoft.CodeAnalysis.INamedTypeSymbol");
            }

            var typeName = reader.GetString();
            if (string.IsNullOrEmpty(typeName))
            {
                throw new JsonException("Type must not be null");
            }

            return _compilation.GetTypeByMetadataName(typeName);
        }

        public override void Write(Utf8JsonWriter writer, INamedTypeSymbol value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToDisplayString());
    }
}
