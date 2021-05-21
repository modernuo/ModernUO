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

            var version = classSymbol.GetAttributes()
                .FirstOrDefault(
                    attr => attr.AttributeClass?.Equals(serializableEntityAttribute, SymbolEqualityComparer.Default) ?? false
                )?.ConstructorArguments.FirstOrDefault().Value?.ToString();

            if (version == null)
            {
                return null; // We don't have the attribute
            }

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
                version,
                true
            );
            source.AppendLine();

            var serializableFields = new List<IFieldSymbol>();
            var migrationProperties = new List<SerializableProperty>();

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
                    serializableFields.Add(fieldSymbol);

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

                    migrationProperties.Add(new SerializableProperty
                    {
                        Name = fieldSymbol.GetPropertyName(),
                        Type = fieldSymbol.Type.ToDisplayString(),
                        ReadMethod = ((INamedTypeSymbol)fieldSymbol.Type).GetDeserializeReaderMethod(
                            compilation,
                            allAttributes,
                            serializableTypes
                        )
                    });
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

            var fieldsArray = serializableFields.ToImmutableArray();

            // Serialize Method
            source.GenerateSerializeMethod(
                compilation,
                isOverride,
                fieldsArray,
                serializableTypes
            );
            source.AppendLine();

            var versionValue = int.Parse(version);
            List<SerializableMetadata> migrations;

            if (versionValue > 0)
            {
                migrations = SerializableMigration.GetMigrations(
                    migrationPath,
                    classSymbol,
                    versionValue,
                    jsonSerializerOptions
                );

                for (var i = 0; i < migrations.Count; i++)
                {
                    var migration = migrations[i];
                    source.GenerateMigrationContentStruct(migration);
                    source.AppendLine();
                }
            }
            else
            {
                migrations = new List<SerializableMetadata>();
            }

            // Deserialize Method
            source.GenerateDeserializeMethod(
                compilation,
                isOverride,
                versionValue,
                migrations,
                fieldsArray,
                serializableTypes
            );

            source.GenerateClassEnd();
            source.GenerateNamespaceEnd();

            // Write the migration file
            var newMigration = new SerializableMetadata
            {
                Version = versionValue,
                Type = classSymbol.ToDisplayString(),
                Properties = migrationProperties
            };
            SerializableMigration.WriteMigration(migrationPath, newMigration, jsonSerializerOptions);

            return source.ToString();
        }
    }
}
