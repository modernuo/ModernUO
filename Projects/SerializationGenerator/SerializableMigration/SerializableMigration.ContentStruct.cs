/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableMigration.ContentStruct.cs                          *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SerializationGenerator
{
    public static partial class SerializableMigration
    {
        public static void GenerateMigrationContentStruct(
            this StringBuilder source,
            Compilation compilation,
            int version,
            ImmutableArray<IFieldSymbol> fields,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {
            const string indent = "            ";

            source.AppendLine($"{indent}struct ContentV{version}");
            source.AppendLine($"{indent}{{");
            foreach (var field in fields)
            {
                source.AppendLine($"{indent}    protected {field.Type} {field.GetPropertyName()}");
            }

            source.AppendLine($"{indent}    public ContentV{version}(IGenericReader reader)");
            source.AppendLine($"{indent}    {{");

            foreach (var field in fields)
            {
                var serializableProperty = new SerializableProperty
                {
                    Name = field.GetPropertyName(),
                    Type = (INamedTypeSymbol)field.Type
                };

                source.DeserializeField(
                    $"{indent}        ",
                    serializableProperty,
                    compilation,
                    ImmutableArray<AttributeData>.Empty,
                    serializableTypes
                );
            }

            source.AppendLine($"{indent}    }}");
        }
    }
}
