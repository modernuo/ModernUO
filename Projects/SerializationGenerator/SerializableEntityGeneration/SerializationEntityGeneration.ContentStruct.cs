/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializationEntityGeneration.ContentStruct.cs                  *
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
using SerializableMigration;

namespace SerializationGenerator
{
    public static partial class SerializableEntityGeneration
    {
        public static void GenerateMigrationContentStruct(
            this StringBuilder source,
            Compilation compilation,
            string indent,
            SerializableMetadata migration,
            INamedTypeSymbol classSymbol
        )
        {
            source.AppendLine($"{indent}ref struct V{migration.Version}Content");
            source.AppendLine($"{indent}{{");
            var properties = migration.Properties ?? ImmutableArray<SerializableProperty>.Empty;

            foreach (var serializableProperty in properties)
            {
                var propertyType = serializableProperty.Type;
                var type = compilation.GetTypeByMetadataName(propertyType)?.IsValueType == true
                           || SymbolMetadata.IsPrimitiveFromTypeDisplayString(propertyType) && propertyType != "bool"
                    ? $"{propertyType}?" : propertyType;

                source.AppendLine($"{indent}    internal readonly {type} {serializableProperty.Name};");
            }

            var innerIndent = $"{indent}        ";

            var usesSaveFlags = properties.Any(p => p.UsesSaveFlag == true);

            if (usesSaveFlags)
            {
                source.AppendLine();
                source.GenerateEnumStart(
                    $"V{migration.Version}SaveFlag",
                    $"{indent}    ",
                    true,
                    Accessibility.Private
                );

                source.GenerateEnumValue(innerIndent, true, "None", -1);
                int index = 0;
                foreach (var property in properties)
                {
                    if (property.UsesSaveFlag == true)
                    {
                        source.GenerateEnumValue(innerIndent, true, property.Name, index++);
                    }
                }

                source.GenerateEnumEnd($"{indent}    ");
            }

            source.AppendLine($"{indent}    internal V{migration.Version}Content(IGenericReader reader, {classSymbol.ToDisplayString()} entity)");
            source.AppendLine($"{indent}    {{");

            if (usesSaveFlags)
            {
                source.AppendLine($"{innerIndent}var saveFlags = reader.ReadEnum<V{migration.Version}SaveFlag>();");
            }

            if (properties.Length > 0)
            {
                foreach (var property in properties)
                {
                    if (property.UsesSaveFlag == true)
                    {
                        source.AppendLine();
                        // Special case
                        if (property.Type == "bool")
                        {
                            source.AppendLine($"{innerIndent}{property.Name} = (saveFlags & V{migration.Version}SaveFlag.{property.Name}) != 0;");
                        }
                        else
                        {
                            source.AppendLine($"{innerIndent}if ((saveFlags & V{migration.Version}SaveFlag.{property.Name}) != 0)\n{innerIndent}{{");

                            SerializableMigrationRulesEngine.Rules[property.Rule].GenerateDeserializationMethod(
                                source,
                                $"{innerIndent}    ",
                                property,
                                "entity"
                            );

                            source.AppendLine($"{innerIndent}}}\n{innerIndent}else\n{innerIndent}{{");
                            source.AppendLine($"{innerIndent}    {property.Name} = default;");
                            source.AppendLine($"{innerIndent}}}");
                        }
                    }
                    else
                    {
                        SerializableMigrationRulesEngine.Rules[property.Rule].GenerateDeserializationMethod(
                            source,
                            innerIndent,
                            property,
                            "entity"
                        );
                    }
                }
            }

            source.AppendLine($"{indent}    }}");

            source.AppendLine($"{indent}}}");
        }
    }
}
