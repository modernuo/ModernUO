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

using System.Collections.Generic;
using System.Collections.Immutable;
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
            List<SerializableMetadata> migrations,
            List<SerializableProperty> properties
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

            if (version > 0)
            {
                var nextVersion = 0;

                for (var i = 0; i < migrations.Count; i++)
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

            source.GenerateMethodEnd();
        }
    }
}
