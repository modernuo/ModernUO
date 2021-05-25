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
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace SerializationGenerator
{
    public static partial class SerializableEntityGeneration
    {
        public static bool WillBeSerializable(this INamedTypeSymbol classSymbol, GeneratorExecutionContext context)
        {
            var compilation = context.Compilation;

            var serializableEntityAttribute =
                compilation.GetTypeByMetadataName(SERIALIZABLE_ATTRIBUTE);
            var serializableInterface = compilation.GetTypeByMetadataName(SERIALIZABLE_INTERFACE);

            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return false;
            }

            if (!classSymbol.ContainsInterface(serializableInterface))
            {
                return false;
            }

            var versionValue = classSymbol.GetAttributes()
                .FirstOrDefault(
                    attr => attr.AttributeClass?.Equals(serializableEntityAttribute, SymbolEqualityComparer.Default) ?? false
                )?.ConstructorArguments.FirstOrDefault().Value;

            return versionValue != null;
        }

        public static string GenerateSerializationPartialClass(
            INamedTypeSymbol classSymbol,
            IList<IFieldSymbol> fields,
            GeneratorExecutionContext context,
            string migrationPath,
            JsonSerializerOptions jsonSerializerOptions,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {
            var compilation = context.Compilation;

            var serializableEntityAttribute =
                compilation.GetTypeByMetadataName(SERIALIZABLE_ATTRIBUTE);
            var serializableFieldAttribute =
                compilation.GetTypeByMetadataName(SERIALIZABLE_FIELD_ATTRIBUTE);
            var serializableFieldAttrAttribute =
                compilation.GetTypeByMetadataName(SERIALIZABLE_FIELD_ATTR_ATTRIBUTE);
            var serializableInterface = compilation.GetTypeByMetadataName(SERIALIZABLE_INTERFACE);

            // This is a class symbol if the containing symbol is the namespace
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return null;
            }

            // If we have a parent that is or derives from ISerializable, then we are in override
            var isOverride = classSymbol.BaseType.ContainsInterface(serializableInterface);

            if (!isOverride && !classSymbol.ContainsInterface(serializableInterface))
            {
                return null;
            }

            var serializableAttribute = classSymbol.GetAttributes()
                .FirstOrDefault(
                    attr => attr.AttributeClass?.Equals(serializableEntityAttribute, SymbolEqualityComparer.Default) ?? false
                );

            var version = (int)serializableAttribute?.ConstructorArguments[0].Value!;
            var encodedVersion = (bool)serializableAttribute?.ConstructorArguments[1].Value!;

            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;

            StringBuilder source = new StringBuilder();

            source.GenerateNamespaceStart(namespaceName);

            source.GenerateClassStart(
                className,
                isOverride ?
                    ImmutableArray<ITypeSymbol>.Empty :
                    ImmutableArray.Create<ITypeSymbol>(serializableInterface)
            );

            source.GenerateClassField(
                AccessModifier.Private,
                InstanceModifier.Const,
                "int",
                "_version",
                version.ToString(),
                true
            );
            source.AppendLine();

            var serializableProperties = new List<SerializableProperty>();

            foreach (IFieldSymbol fieldSymbol in fields)
            {
                var allAttributes = fieldSymbol.GetAttributes();

                var hasAttribute = allAttributes
                    .Any(
                        attr =>
                            SymbolEqualityComparer.Default.Equals(attr.AttributeClass, serializableFieldAttribute)
                    );

                if (hasAttribute)
                {
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
                            source.GenerateAttribute(attrType.Name, ctorArgs[1].Values);
                        }
                    }

                    source.GenerateSerializableProperty(fieldSymbol);
                    source.AppendLine();

                    var serializableProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
                        compilation,
                        fieldSymbol.GetPropertyName(),
                        fieldSymbol.Type,
                        allAttributes,
                        serializableTypes
                    );

                    serializableProperties.Add(serializableProperty);
                }
            }

            // If we are not inheriting ISerializable, then we need to define some stuff
            if (!isOverride)
            {
                // long ISerializable.SavePosition { get; set; }
                source.GenerateAutoProperty(
                    AccessModifier.None,
                    "long",
                    "ISerializable.SavePosition",
                    AccessModifier.None,
                    AccessModifier.None
                );
                source.AppendLine();

                // BufferWriter ISerializable.SaveBuffer { get; set; }
                source.GenerateAutoProperty(
                    AccessModifier.None,
                    "BufferWriter",
                    "ISerializable.SaveBuffer",
                    AccessModifier.None,
                    AccessModifier.None
                );
                source.AppendLine();
            }

            // Serial constructor
            source.GenerateSerialCtor(context, className, isOverride);
            source.AppendLine();

            List<SerializableMetadata> migrations;

            if (version > 0)
            {
                migrations = SerializableMigration.GetMigrations(
                    migrationPath,
                    classSymbol,
                    version,
                    jsonSerializerOptions
                );

                for (var i = 0; i < migrations.Count; i++)
                {
                    var migration = migrations[i];
                    if (migration.Version < version)
                    {
                        source.GenerateMigrationContentStruct(migration);
                        source.AppendLine();
                    }
                }
            }
            else
            {
                migrations = new List<SerializableMetadata>();
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

            // Write the migration file
            var newMigration = new SerializableMetadata
            {
                Version = version,
                Type = classSymbol.ToDisplayString(),
                Properties = serializableProperties
            };
            SerializableMigration.WriteMigration(migrationPath, newMigration, jsonSerializerOptions);

            return source.ToString();
        }
    }
}
