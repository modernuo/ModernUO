/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableEntityGeneration.SerializeMethod.cs                 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using SerializableMigration;

namespace SerializationGenerator
{
    public static partial class SerializableEntityGeneration
    {
        public static void GenerateSerializeMethod(
            this StringBuilder source,
            Compilation compilation,
            bool isOverride,
            bool encodedVersion,
            ImmutableArray<SerializableProperty> properties,
            SortedDictionary<int, SerializableFieldSaveFlagMethods> serializableFieldSaveFlagMethodsDictionary
        )
        {
            var genericWriterInterface = compilation.GetTypeByMetadataName(SymbolMetadata.GENERIC_WRITER_INTERFACE);

            source.GenerateMethodStart(
                "        ",
                "Serialize",
                Accessibility.Public,
                isOverride,
                "void",
                ImmutableArray.Create<(ITypeSymbol, string)>((genericWriterInterface, "writer"))
            );

            const string indent = "            ";
            const string innerIndent = $"{indent}    ";

            if (isOverride)
            {
                source.AppendLine($"{indent}base.Serialize(writer);");
                source.AppendLine();
            }

            // Version
            source.AppendLine($"{indent}writer.{(encodedVersion ? "WriteEncodedInt" : "Write")}(_version);");

            // Let's collect the flags
            if (serializableFieldSaveFlagMethodsDictionary.Count > 0)
            {
                source.AppendLine($"\n{indent}var saveFlags = SaveFlag.None;");

                foreach (var (order, saveFlagMethods) in serializableFieldSaveFlagMethodsDictionary)
                {
                    source.AppendLine($"{indent}if ({saveFlagMethods.DetermineFieldShouldSerialize!.Name}())\n{indent}{{");

                    var propertyName = properties[order].Name;
                    source.AppendLine($"{innerIndent}saveFlags |= SaveFlag.{propertyName};");

                    source.AppendLine($"{indent}}}");
                }

                source.AppendLine($"{indent}writer.WriteEnum(saveFlags);");
            }

            foreach (var property in properties)
            {
                if (serializableFieldSaveFlagMethodsDictionary.ContainsKey(property.Order))
                {
                    // Special case
                    if (property.Type != "bool")
                    {
                        source.AppendLine($"\n{indent}if ((saveFlags & SaveFlag.{property.Name}) != 0)\n{indent}{{");
                        SerializableMigrationRulesEngine.Rules[property.Rule].GenerateSerializationMethod(
                            source,
                            innerIndent,
                            property
                        );
                        source.AppendLine($"{indent}}}");
                    }
                }
                else
                {
                    source.AppendLine();
                    SerializableMigrationRulesEngine.Rules[property.Rule].GenerateSerializationMethod(
                        source,
                        indent,
                        property
                    );
                }
            }

            source.GenerateMethodEnd("        ");
        }
    }
}
