/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializationGenerator.ContentStruct.cs                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

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
            SerializableMetadata migration,
            INamedTypeSymbol classSymbol
        )
        {
            const string indent = "        ";

            source.AppendLine($"{indent}ref struct V{migration.Version}Content");
            source.AppendLine($"{indent}{{");
            foreach (var serializableProperty in migration.Properties)
            {
                source.AppendLine($"{indent}    internal readonly {serializableProperty.Type} {serializableProperty.Name};");
            }

            var innerIndent = $"{indent}        ";

            var usesSaveFlags = migration.Properties.Any(p => p.UsesSaveFlag == true);

            if (usesSaveFlags)
            {
                source.AppendLine();
                source.GenerateEnumStart(
                    $"V{migration.Version}SaveFlag",
                    $"{indent}    ",
                    true,
                    Accessibility.Private
                );

                int index = 0;
                source.GenerateEnumValue(innerIndent, true, "None", index++);
                foreach (var property in migration.Properties)
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

            if (migration.Properties.Length > 0)
            {
                source.AppendLine();
                foreach (var property in migration.Properties)
                {
                    if (property.UsesSaveFlag == true)
                    {
                        source.AppendLine($"\n{innerIndent}if ((saveFlags & V{migration.Version}SaveFlag.{property.Name}) != 0)\n{innerIndent}{{");
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
