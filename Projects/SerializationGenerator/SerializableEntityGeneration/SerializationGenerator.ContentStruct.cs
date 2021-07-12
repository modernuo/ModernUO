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

using System.Text;
using SerializableMigration;

namespace SerializationGenerator
{
    public static partial class SerializableEntityGeneration
    {
        public static void GenerateMigrationContentStruct(
            this StringBuilder source,
            SerializableMetadata migration
        )
        {
            const string indent = "        ";

            source.AppendLine($"{indent}ref struct V{migration.Version}Content");
            source.AppendLine($"{indent}{{");
            foreach (var serializableProperty in migration.Properties)
            {
                source.AppendLine($"{indent}    internal readonly {serializableProperty.Type} {serializableProperty.Name};");
            }

            source.AppendLine($"{indent}    internal V{migration.Version}Content(IGenericReader reader)");
            source.AppendLine($"{indent}    {{");

            foreach (var serializableProperty in migration.Properties)
            {
                SerializableMigrationRulesEngine.Rules[serializableProperty.Rule].GenerateDeserializationMethod(
                    source,
                    $"{indent}        ",
                    serializableProperty
                );
            }

            source.AppendLine($"{indent}    }}");

            source.AppendLine($"{indent}}}");
        }
    }
}
