/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableEntityGeneration.Class.cs                           *
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
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using SerializableMigration;

namespace SerializationGenerator
{
    public static partial class SerializableEntityGeneration
    {
        public static string GenerateSerializationPartialClass(
            this GeneratorExecutionContext context,
            INamedTypeSymbol classSymbol,
            AttributeData serializableAttr,
            ImmutableArray<ISymbol> fieldsAndProperties,
            JsonSerializerOptions jsonSerializerOptions,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {
            var version = (int)serializableAttr.ConstructorArguments[0].Value!;

            var migrations = context.GetMigrationsByAnalyzerConfig(
                classSymbol,
                version,
                jsonSerializerOptions
            );

            return context.Compilation.GenerateSerializationPartialClass(
                classSymbol,
                serializableAttr,
                null, // Do not generate schema
                null,
                migrations.ToImmutableArray(),
                fieldsAndProperties,
                serializableTypes
            );
        }

        public static string GenerateSerializationPartialClass(
            this Compilation compilation,
            INamedTypeSymbol classSymbol,
            AttributeData serializableAttr,
            string? migrationPath,
            JsonSerializerOptions? jsonSerializerOptions,
            ImmutableArray<ISymbol> fieldsAndProperties,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {
            var version = (int)serializableAttr.ConstructorArguments[0].Value!;

            var migrations = SerializableMigrationSchema.GetMigrations(
                classSymbol,
                version,
                migrationPath,
                jsonSerializerOptions
            );

            return compilation.GenerateSerializationPartialClass(
                classSymbol,
                serializableAttr,
                migrationPath,
                jsonSerializerOptions,
                migrations.ToImmutableArray(),
                fieldsAndProperties,
                serializableTypes
            );
        }

        public static string GenerateSerializationPartialClass(
            this Compilation compilation,
            INamedTypeSymbol classSymbol,
            AttributeData serializableAttr,
            string? migrationPath,
            JsonSerializerOptions? jsonSerializerOptions,
            ImmutableArray<SerializableMetadata> migrations,
            ImmutableArray<ISymbol> fieldsAndProperties,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {
            var serializableFieldAttribute =
                compilation.GetTypeByMetadataName(SymbolMetadata.SERIALIZABLE_FIELD_ATTRIBUTE);
            var serializableFieldAttrAttribute =
                compilation.GetTypeByMetadataName(SymbolMetadata.SERIALIZABLE_FIELD_ATTR_ATTRIBUTE);
            var serializableInterface = compilation.GetTypeByMetadataName(SymbolMetadata.SERIALIZABLE_INTERFACE);

            // If we have a parent that is or derives from ISerializable, then we are in override
            var isOverride = classSymbol.BaseType.ContainsInterface(serializableInterface);

            if (!isOverride && !classSymbol.ContainsInterface(serializableInterface))
            {
                return null;
            }

            var version = (int)serializableAttr.ConstructorArguments[0].Value!;
            var encodedVersion = (bool)serializableAttr.ConstructorArguments[1].Value!;

            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;

            StringBuilder source = new StringBuilder();

            source.AppendLine("#pragma warning disable\n");
            source.GenerateNamespaceStart(namespaceName);

            source.GenerateClassStart(
                className,
                ImmutableArray<ITypeSymbol>.Empty
            );

            const string indent = "        ";

            source.GenerateClassField(
                Accessibility.Private,
                InstanceModifier.Const,
                "int",
                "_version",
                version.ToString()
            );
            source.AppendLine();

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

                foreach (var attr in allAttributes)
                {
                    if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, serializableFieldAttrAttribute))
                    {
                        continue;
                    }

                    if (attr.AttributeClass == null)
                    {
                        continue;
                    }

                    var ctorArgs = attr.ConstructorArguments;
                    var attrTypeArg = ctorArgs[0];

                    if (attrTypeArg.Kind == TypedConstantKind.Primitive && attrTypeArg.Value is string attrStr)
                    {
                        source.AppendLine($"        {attrStr}");
                    }
                    else
                    {
                        var attrType = (ITypeSymbol)attrTypeArg.Value;
                        source.GenerateAttribute(attrType?.Name, ctorArgs[1].Values);
                    }
                }

                var attrCtorArgs = serializableFieldAttr.ConstructorArguments;

                var order = (int)attrCtorArgs[0].Value!;
                var getterAccessor = Helpers.GetAccessibility(attrCtorArgs[1].Value!.ToString());
                var setterAccessor = Helpers.GetAccessibility(attrCtorArgs[2].Value!.ToString());
                var virtualProperty = (bool)attrCtorArgs[3].Value!;

                if (fieldOrPropertySymbol is IFieldSymbol fieldSymbol)
                {
                    source.GenerateSerializableProperty(
                        compilation,
                        fieldSymbol,
                        getterAccessor,
                        setterAccessor,
                        virtualProperty
                    );
                    source.AppendLine();
                }

                var serializableProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
                    compilation,
                    fieldOrPropertySymbol,
                    order,
                    allAttributes,
                    serializableTypes,
                    classSymbol
                );

                serializablePropertySet.Add(serializableProperty);
            }

            var serializableProperties = serializablePropertySet.ToImmutableArray();

            // If we are not inheriting ISerializable, then we need to define some stuff
            if (!isOverride)
            {
                // long ISerializable.SavePosition { get; set; } = -1;
                source.GenerateAutoProperty(
                    Accessibility.NotApplicable,
                    "long",
                    "ISerializable.SavePosition",
                    Accessibility.NotApplicable,
                    Accessibility.NotApplicable,
                    indent,
                    defaultValue: "-1"
                );

                // BufferWriter ISerializable.SaveBuffer { get; set; }
                source.GenerateAutoProperty(
                    Accessibility.NotApplicable,
                    "BufferWriter",
                    "ISerializable.SaveBuffer",
                    Accessibility.NotApplicable,
                    Accessibility.NotApplicable,
                    indent
                );
            }

            // Serial constructor
            source.GenerateSerialCtor(compilation, className, isOverride);
            source.AppendLine();

            if (version > 0)
            {
                for (var i = 0; i < migrations.Length; i++)
                {
                    var migration = migrations[i];
                    if (migration.Version < version)
                    {
                        source.GenerateMigrationContentStruct(migration);
                        source.AppendLine();
                    }
                }
            }

            // Serialize Method
            source.GenerateSerializeMethod(
                compilation,
                isOverride,
                encodedVersion,
                serializableProperties
            );
            source.AppendLine();

            // Deserialize Method
            source.GenerateDeserializeMethod(
                compilation,
                classSymbol,
                isOverride,
                version,
                encodedVersion,
                migrations,
                serializableProperties
            );

            source.GenerateClassEnd();
            source.GenerateNamespaceEnd();

            if (migrationPath != null)
            {
                // Write the migration file
                var newMigration = new SerializableMetadata
                {
                    Version = version,
                    Type = classSymbol.ToDisplayString(),
                    Properties = serializableProperties
                };

                WriteMigration(migrationPath, newMigration, jsonSerializerOptions);
            }

            return source.ToString();
        }

        private static void WriteMigration(string migrationPath, SerializableMetadata metadata, JsonSerializerOptions options)
        {
            Directory.CreateDirectory(migrationPath);
            var filePath = Path.Combine(migrationPath, $"{metadata.Type}.v{metadata.Version}.json");
            File.WriteAllText(filePath, JsonSerializer.Serialize(metadata, options));
        }
    }
}
