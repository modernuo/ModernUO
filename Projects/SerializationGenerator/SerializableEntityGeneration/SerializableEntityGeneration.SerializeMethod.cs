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
using System.Text;
using Microsoft.CodeAnalysis;
using SerializableMigration;

namespace SerializationGenerator;

public static partial class SerializableEntityGeneration
{
    public static void GenerateSerializeMethod(
        this StringBuilder source,
        Compilation compilation,
        string indent,
        bool isOverride,
        bool encodedVersion,
        ImmutableArray<SerializableProperty> fields,
        ImmutableArray<SerializableProperty> properties,
        SortedDictionary<int, SerializableFieldSaveFlagMethods> serializableFieldSaveFlagMethodsDictionary
    )
    {
        var genericWriterInterface = compilation.GetTypeByMetadataName(SymbolMetadata.GENERIC_WRITER_INTERFACE);

        source.GenerateMethodStart(
            indent,
            "Serialize",
            Accessibility.Public,
            isOverride,
            "void",
            ImmutableArray.Create<(ITypeSymbol, string)>((genericWriterInterface, "writer"))
        );

        var bodyIndent = $"{indent}    ";
        var innerIndent = $"{bodyIndent}    ";

        if (isOverride)
        {
            source.AppendLine($"{bodyIndent}base.Serialize(writer);");
            source.AppendLine();
        }

        // Version
        source.AppendLine($"{bodyIndent}writer.{(encodedVersion ? "WriteEncodedInt" : "Write")}(_version);");

        // Let's collect the flags
        if (serializableFieldSaveFlagMethodsDictionary.Count > 0)
        {
            source.AppendLine($"\n{bodyIndent}var saveFlags = SaveFlag.None;");

            foreach (var (order, saveFlagMethods) in serializableFieldSaveFlagMethodsDictionary)
            {
                source.AppendLine($"{bodyIndent}if ({saveFlagMethods.DetermineFieldShouldSerialize!.Name}())\n{bodyIndent}{{");

                var propertyName = properties[order].Name;
                source.AppendLine($"{innerIndent}saveFlags |= SaveFlag.{propertyName};");

                source.AppendLine($"{bodyIndent}}}");
            }

            source.AppendLine($"{bodyIndent}writer.WriteEnum(saveFlags);");
        }

        for (var i = 0; i < properties.Length; i++)
        {
            var field = fields[i];
            var property = properties[i];
            if (serializableFieldSaveFlagMethodsDictionary.ContainsKey(property.Order))
            {
                // Special case
                if (property.Type != "bool")
                {
                    source.AppendLine($"\n{bodyIndent}if ((saveFlags & SaveFlag.{property.Name}) != 0)\n{bodyIndent}{{");
                    SerializableMigrationRulesEngine.Rules[property.Rule]
                        .GenerateSerializationMethod(
                            source,
                            innerIndent,
                            field
                        );
                    source.AppendLine($"{bodyIndent}}}");
                }
            }
            else
            {
                source.AppendLine();
                SerializableMigrationRulesEngine.Rules[property.Rule]
                    .GenerateSerializationMethod(
                        source,
                        bodyIndent,
                        field
                    );
            }
        }

        source.GenerateMethodEnd(indent);
    }
}