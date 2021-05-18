/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableMigration.cs                                        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Server.Json;

namespace SerializationGenerator
{
    public static partial class SerializableMigration
    {
        public static JsonSerializerOptions GetJsonSerializerOptions(Compilation compilation)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                AllowTrailingCommas = true,
                IgnoreNullValues = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            options.Converters.Add(new NamedTypeSymbolConverterFactory(compilation));

            return options;
        }

        public static string GetMigrationPath(GeneratorExecutionContext context)
        {
            string path = null;

            foreach (var file in context.AdditionalFiles)
            {
                if (context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.MigrationPath", out var migrationPath)
                    && migrationPath.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    path = Path.GetDirectoryName(file.Path);
                    break;
                }
            }

            return path;
        }

        public static List<SerializableMetadata> GetMigrations(
            string migrationPath,
            INamedTypeSymbol typeSymbol,
            int version,
            JsonSerializerOptions options
        )
        {
            var typeName = typeSymbol.ToDisplayString();

            var migrations = new SortedSet<SerializableMetadata>(new SerializableMetadataComparer());
            var migrationFiles = Directory.GetFiles(migrationPath, $"{typeName}.v*.json");

            foreach (var migrationFile in migrationFiles)
            {
                var text = File.ReadAllText(migrationFile, Encoding.UTF8);
                var migration = JsonSerializer.Deserialize<SerializableMetadata>(text, options);
                if (migration != null && typeName == migration.Type.ToDisplayString() && version > migration.Version)
                {
                    migrations.Add(migration);
                }
            }

            return migrations.ToList();
        }

        public static void WriteMigration(string migrationPath, SerializableMetadata metadata, JsonSerializerOptions options)
        {
            Directory.CreateDirectory(migrationPath);
            var filePath = Path.Combine(migrationPath, $"{metadata.Type.Name}.v{metadata.Version}.json");
            File.WriteAllText(filePath, JsonSerializer.Serialize(metadata, options));
        }
    }
}
