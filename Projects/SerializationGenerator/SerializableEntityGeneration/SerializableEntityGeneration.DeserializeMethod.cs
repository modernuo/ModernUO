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
            ImmutableArray<SerializableProperty> properties
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

            if (isOverride)
            {
                source.AppendLine($"{indent}base.Deserialize(reader);");
                source.AppendLine();
            }

            // Version
            source.AppendLine($"{indent}var version = reader.{(encodedVersion ? "ReadEncodedInt" : "ReadInt")}();");

            if (version > 0)
            {
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
                    source.AppendLine($"{indent}    MigrateFrom(new V{migrationVersion}Content(reader));");
                    source.AppendLine($"{indent}    ((Server.ISerializable)this).MarkDirty();");
                    source.AppendLine($"{indent}    return;");
                    source.AppendLine($"{indent}}}");
                }

                if (nextVersion < version)
                {
                    source.AppendLine();
                    source.AppendLine($"{indent}if (version < _version)");
                    source.AppendLine($"{indent}{{");
                    source.AppendLine($"{indent}    Deserialize(reader, version);");
                    source.AppendLine($"{indent}    ((Server.ISerializable)this).MarkDirty();");
                    source.AppendLine($"{indent}    return;");
                    source.AppendLine($"{indent}}}");
                }
            }

            foreach (var property in properties)
            {
                source.AppendLine();
                SerializableMigrationRulesEngine.Rules[property.Rule].GenerateDeserializationMethod(
                    source,
                    indent,
                    property
                );
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

            if (afterDeserialization != null)
            {
                source.AppendLine();
                source.AppendLine($"{indent}Timer.DelayCall({afterDeserialization.Name});");
            }

            source.GenerateMethodEnd("        ");
        }
    }
}
