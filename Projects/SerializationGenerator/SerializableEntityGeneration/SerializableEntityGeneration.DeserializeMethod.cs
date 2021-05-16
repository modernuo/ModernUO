/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableEntityGeneration.DeserializeMethod.cs               *
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
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SerializationGenerator
{
    public static partial class SerializableEntityGeneration
    {
        public static void GenerateDeserializeMethod(
            this StringBuilder source,
            Compilation compilation,
            bool isOverride,
            int version,
            ImmutableArray<IFieldSymbol> fields,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {
            var genericReaderInterface = compilation.GetTypeByMetadataName(GENERIC_READER_INTERFACE);

            source.GenerateMethodStart(
                "Deserialize",
                AccessModifier.Public,
                isOverride,
                "void",
                ImmutableArray.Create<(ITypeSymbol, string)>((genericReaderInterface, "reader"))
            );

            const string indent = "            ";

            // Version
            source.AppendLine($"{indent}var version = reader.ReadEncodedInt();");
            for (var i = 0; i < version; i++)
            {
                source.AppendLine($"{indent}if (version == {i})");
                source.AppendLine($@"{indent}{{");
                source.AppendLine($"{indent}    MigrateFrom(new Version{i}Content(reader));");
                source.AppendLine($"{indent}    MarkDirty();");
                source.AppendLine($"{indent}    return;");
                source.AppendLine($@"{indent}}}");
            }

            var serializableProperties = fields.Select(
                f => new SerializableProperty
                {
                    Name = f.ToString(),
                    Type = (INamedTypeSymbol)f.Type
                }
            ).ToImmutableArray();

            foreach (var field in fields)
            {
                var serializableProperty = new SerializableProperty
                {
                    Name = field.GetPropertyName(),
                    Type = (INamedTypeSymbol)field.Type
                };

                source.DeserializeField($"{indent}    ", serializableProperty, compilation, serializableTypes);
            }

            source.GenerateMethodEnd();
        }

        public static void DeserializeField(
            this StringBuilder source,
            string indent,
            SerializableProperty property,
            Compilation compilation,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {

        }
    }
}
