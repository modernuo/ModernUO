/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SchemaGenerator.cs                                              *
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
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using SerializableMigration;
using SerializationGenerator;

namespace SerializationSchemaGenerator
{
    public static class SchemaGenerator
    {
        public static void GenerateSchema(
            this Compilation compilation,
            INamedTypeSymbol classSymbol,
            AttributeData serializableAttr,
            ImmutableArray<ISymbol> fieldsAndProperties,
            string migrationPath,
            JsonSerializerOptions jsonSerializerOptions,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {
            var serializableFieldAttribute =
                compilation.GetTypeByMetadataName(SymbolMetadata.SERIALIZABLE_FIELD_ATTRIBUTE);

            var version = (int)serializableAttr.ConstructorArguments[0].Value!;

            var serializablePropertySet = new SortedSet<SerializableProperty>(new SerializablePropertyComparer());

            foreach (var fieldOrPropertySymbol in fieldsAndProperties)
            {
                var allAttributes = fieldOrPropertySymbol.GetAttributes();

                var serializableFieldAttr = allAttributes
                    .FirstOrDefault(
                        attr =>
                            SymbolEqualityComparer.Default.Equals(attr.AttributeClass, serializableFieldAttribute)
                    );

                if (serializableFieldAttr == null)
                {
                    continue;
                }

                var order = (int)serializableFieldAttr.ConstructorArguments[0].Value!;

                var serializableProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
                    compilation,
                    fieldOrPropertySymbol,
                    order,
                    allAttributes,
                    serializableTypes
                );

                if (serializableProperty == null)
                {
                    continue;
                }

                serializablePropertySet.Add(serializableProperty);
            }

            var serializableProperties = serializablePropertySet.ToImmutableArray();

            // Write the migration file
            var newMigration = new SerializableMetadata
            {
                Version = version,
                Type = classSymbol.ToDisplayString(),
                Properties = serializableProperties
            };
            WriteMigration(migrationPath, newMigration, jsonSerializerOptions);
        }

        public static void WriteMigration(string migrationPath, SerializableMetadata metadata, JsonSerializerOptions options)
        {
            Directory.CreateDirectory(migrationPath);
            var filePath = Path.Combine(migrationPath, $"{metadata.Type}.v{metadata.Version}.json");
            File.WriteAllText(filePath, JsonSerializer.Serialize(metadata, options));
        }
    }
}
