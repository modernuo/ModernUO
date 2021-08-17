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
using SerializableMigration;

namespace SerializationGenerator
{
    public static partial class SerializableEntityGeneration
    {
        public static void GenerateDeserializeMethod(
            this StringBuilder source,
            Compilation compilation,
            INamedTypeSymbol classSymbol,
            bool isOverride,
            int version,
            bool encodedVersion,
            ImmutableArray<SerializableMetadata> migrations,
            ImmutableArray<SerializableProperty> properties,
            ISymbol parentFieldOrProperty,
            ImmutableArray<(IMethodSymbol, int)> propertyFlagGetters
        )
        {
            var genericReaderInterface = compilation.GetTypeByMetadataName(SymbolMetadata.GENERIC_READER_INTERFACE);

            source.GenerateMethodStart(
                "        ",
                "Deserialize",
                Accessibility.Public,
                isOverride,
                "void",
                ImmutableArray.Create<(ITypeSymbol, string)>((genericReaderInterface, "reader"))
            );

            const string indent = "            ";
            const string innerIndent = $"{indent}    ";

            if (isOverride)
            {
                source.AppendLine($"{indent}base.Deserialize(reader);");
                source.AppendLine();
            }

            var afterDeserialization = classSymbol
                .GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(
                    m =>
                        m.ReturnsVoid &&
                        m.Parameters.Length == 0 &&
                        m.GetAttributes()
                            .Any(
                                attr => SymbolEqualityComparer.Default.Equals(
                                    attr.AttributeClass,
                                    compilation.GetTypeByMetadataName(SymbolMetadata.AFTERDESERIALIZATION_ATTRIBUTE)
                                )
                            )
                );

            // Version
            source.AppendLine($"{indent}var version = reader.{(encodedVersion ? "ReadEncodedInt" : "ReadInt")}();");

            if (version > 0)
            {
                var parent = parentFieldOrProperty?.Name ?? "this";
                var nextVersion = 0;

                for (var i = 0; i < migrations.Length; i++)
                {
                    var migrationVersion = migrations[i].Version;
                    if (migrationVersion == nextVersion)
                    {
                        nextVersion++;
                    }

                    source.AppendLine();
                    source.AppendLine($"{indent}if (version == {migrationVersion})");
                    source.AppendLine($"{indent}{{");
                    source.AppendLine($"{indent}    MigrateFrom(new V{migrationVersion}Content(reader, this));");
                    source.AppendLine($"{indent}    {parent}.MarkDirty();");
                    if (afterDeserialization != null)
                    {
                        source.AppendLine($"{indent}    Timer.DelayCall({afterDeserialization.Name});");
                    }
                    source.AppendLine($"{indent}    return;");
                    source.AppendLine($"{indent}}}");
                }

                if (nextVersion < version)
                {
                    source.AppendLine();
                    source.AppendLine($"{indent}if (version < _version)");
                    source.AppendLine($"{indent}{{");
                    source.AppendLine($"{indent}    Deserialize(reader, version);");
                    source.AppendLine($"{indent}    {parent}.MarkDirty();");
                    if (afterDeserialization != null)
                    {
                        source.AppendLine($"{indent}    Timer.DelayCall({afterDeserialization.Name});");
                    }
                    source.AppendLine($"{indent}    return;");
                    source.AppendLine($"{indent}}}");
                }
            }

            if (propertyFlagGetters.Length > 0)
            {
                source.AppendLine();
                source.AppendLine($"{indent}var saveFlags = reader.ReadEnum<SaveFlag>();");
            }

            foreach (var property in properties)
            {
                var usesSaveFlag = propertyFlagGetters.Any(m => m.Item2 == property.Order);
                var rule = SerializableMigrationRulesEngine.Rules[property.Rule];

                if (usesSaveFlag)
                {
                    source.AppendLine($"\n{indent}if ((saveFlags & SaveFlag.{property.Name}) != 0)\n{indent}{{");
                    rule.GenerateDeserializationMethod(
                        source,
                        innerIndent,
                        property,
                        "this"
                    );
                    (rule as IPostDeserializeMethod)?.PostDeserializeMethod(source, innerIndent, property, compilation, classSymbol);

                    source.AppendLine($"{indent}}}");
                }
                else
                {
                    source.AppendLine();
                    rule.GenerateDeserializationMethod(
                        source,
                        indent,
                        property,
                        "this"
                    );
                    (rule as IPostDeserializeMethod)?.PostDeserializeMethod(source, indent, property, compilation, classSymbol);
                }
            }

            if (afterDeserialization != null)
            {
                source.AppendLine();
                source.AppendLine($"{indent}Timer.DelayCall({afterDeserialization.Name});");
            }

            source.GenerateMethodEnd("        ");
        }
    }
}
