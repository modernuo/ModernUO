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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using SerializableMigration;

namespace SerializationGenerator;

public static partial class SerializableEntityGeneration
{
    public static string GenerateSerializationPartialClass(
        this GeneratorExecutionContext context,
        INamedTypeSymbol classSymbol,
        AttributeData serializableAttr,
        bool embedded,
        ImmutableArray<ISymbol> fieldsAndProperties,
        JsonSerializerOptions jsonSerializerOptions,
        ImmutableArray<INamedTypeSymbol> serializableTypes,
        ImmutableArray<INamedTypeSymbol> embeddedSerializableTypes
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
            embedded,
            null,
            migrations.ToImmutableArray(),
            fieldsAndProperties,
            serializableTypes,
            embeddedSerializableTypes
        );
    }

    public static string GenerateSerializationPartialClass(
        this Compilation compilation,
        INamedTypeSymbol classSymbol,
        AttributeData serializableAttr,
        string? migrationPath,
        bool embedded,
        JsonSerializerOptions? jsonSerializerOptions,
        ImmutableArray<ISymbol> fieldsAndProperties,
        ImmutableArray<INamedTypeSymbol> serializableTypes,
        ImmutableArray<INamedTypeSymbol> embeddedSerializableTypes
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
            embedded,
            jsonSerializerOptions,
            migrations.ToImmutableArray(),
            fieldsAndProperties,
            serializableTypes,
            embeddedSerializableTypes
        );
    }

    public static string GenerateSerializationPartialClass(
        this Compilation compilation,
        INamedTypeSymbol classSymbol,
        AttributeData serializableAttr,
        string? migrationPath,
        bool embedded,
        JsonSerializerOptions? jsonSerializerOptions,
        ImmutableArray<SerializableMetadata> migrations,
        ImmutableArray<ISymbol> fieldsAndProperties,
        ImmutableArray<INamedTypeSymbol> serializableTypes,
        ImmutableArray<INamedTypeSymbol> embeddedSerializableTypes
    )
    {
        var serializableFieldAttribute =
            compilation.GetTypeByMetadataName(SymbolMetadata.SERIALIZABLE_FIELD_ATTRIBUTE);
        var serializableFieldAttrAttribute =
            compilation.GetTypeByMetadataName(SymbolMetadata.SERIALIZABLE_FIELD_ATTR_ATTRIBUTE);
        var serializableInterface =
            compilation.GetTypeByMetadataName(SymbolMetadata.SERIALIZABLE_INTERFACE);
        var parentSerializableAttribute =
            compilation.GetTypeByMetadataName(SymbolMetadata.SERIALIZABLE_PARENT_ATTRIBUTE);
        var serializableFieldSaveFlagAttribute =
            compilation.GetTypeByMetadataName(SymbolMetadata.SERIALIZABLE_FIELD_SAVE_FLAG_ATTRIBUTE);
        var serializableFieldDefaultAttribute =
            compilation.GetTypeByMetadataName(SymbolMetadata.SERIALIZABLE_FIELD_DEFAULT_ATTRIBUTE);

        // If we have a parent that is or derives from ISerializable, then we are in override
        var isOverride = classSymbol.BaseType.ContainsInterface(serializableInterface);

        if (!(embedded || isOverride || classSymbol.ContainsInterface(serializableInterface)))
        {
            return null;
        }

        var isRawSerializable = classSymbol.HasRawSerializableInterface(compilation, ImmutableArray<INamedTypeSymbol>.Empty);

        var version = (int)serializableAttr.ConstructorArguments[0].Value!;
        var encodedVersion = (bool)serializableAttr.ConstructorArguments[1].Value!;

        // Let's find out if we need to do serialization flags
        var serializableFieldSaveFlags = new SortedDictionary<int, SerializableFieldSaveFlagMethods>();
        foreach (var m in classSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            var getSaveFlagAttribute = m.GetAttribute(serializableFieldSaveFlagAttribute);
            var getDefaultValueAttribute = m.GetAttribute(serializableFieldDefaultAttribute);

            if (getSaveFlagAttribute == null && getDefaultValueAttribute == null)
            {
                continue;
            }

            var attrCtorArgs = getSaveFlagAttribute?.ConstructorArguments ?? getDefaultValueAttribute.ConstructorArguments;
            var order = (int)attrCtorArgs[0].Value!;

            serializableFieldSaveFlags.TryGetValue(order, out var saveFlagMethods);

            serializableFieldSaveFlags[order] = new SerializableFieldSaveFlagMethods
            {
                DetermineFieldShouldSerialize = getSaveFlagAttribute != null ? m : saveFlagMethods?.DetermineFieldShouldSerialize,
                GetFieldDefaultValue = getDefaultValueAttribute != null ? m : saveFlagMethods?.GetFieldDefaultValue
            };
        }

        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;

        StringBuilder source = new StringBuilder();

        source.AppendLine("#pragma warning disable\n");
        source.GenerateNamespaceStart(namespaceName);

        var interfaces = !embedded || isRawSerializable
            ? Array.Empty<ITypeSymbol>()
            : new ITypeSymbol[] { compilation.GetTypeByMetadataName(SymbolMetadata.RAW_SERIALIZABLE_INTERFACE) };

        var indent = "    ";

        source.RecursiveGenerateClassStart(classSymbol, interfaces.ToImmutableArray(), ref indent);

        source.GenerateClassField(
            indent,
            Accessibility.Private,
            InstanceModifier.Const,
            "int",
            "_version",
            version.ToString()
        );
        source.AppendLine();

        var parentFieldOrProperty = embedded ? fieldsAndProperties.FirstOrDefault(
            fieldOrPropertySymbol => fieldOrPropertySymbol.GetAttributes()
                .FirstOrDefault(
                    attr =>
                        SymbolEqualityComparer.Default.Equals(attr.AttributeClass, parentSerializableAttribute)
                ) != null
        ) : null;

        var serializablePropertySet = new SortedDictionary<SerializableProperty, ISymbol>(new SerializablePropertyComparer());

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
                    source.AppendLine($"{indent}{attrStr}");
                }
                else
                {
                    var attrType = (ITypeSymbol)attrTypeArg.Value;
                    source.GenerateAttribute(indent, attrType?.Name, ctorArgs[1].Values);
                }
            }

            var attrCtorArgs = serializableFieldAttr.ConstructorArguments;

            var order = (int)attrCtorArgs[0].Value!;
            var getterAccessor = Helpers.GetAccessibility(attrCtorArgs[1].Value?.ToString());
            var setterAccessor = Helpers.GetAccessibility(attrCtorArgs[2].Value?.ToString());
            var virtualProperty = (bool)attrCtorArgs[3].Value!;

            if (fieldOrPropertySymbol is IFieldSymbol fieldSymbol)
            {
                source.GenerateSerializableProperty(
                    compilation,
                    indent,
                    fieldSymbol,
                    getterAccessor,
                    setterAccessor,
                    virtualProperty,
                    parentFieldOrProperty
                );
                source.AppendLine();
            }

            serializableFieldSaveFlags.TryGetValue(order, out var serializableFieldSaveFlagMethods);

            var serializableProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
                compilation,
                fieldOrPropertySymbol,
                order,
                allAttributes,
                serializableTypes,
                embeddedSerializableTypes,
                classSymbol,
                serializableFieldSaveFlagMethods
            );

            serializablePropertySet.Add(serializableProperty, fieldOrPropertySymbol);
        }

        var serializableFields = serializablePropertySet.Keys.ToImmutableArray();
        var serializableProperties = serializablePropertySet.Select(
            kvp => kvp.Key with
            {
                Name = (kvp.Value as IFieldSymbol)?.GetPropertyName() ?? ((IPropertySymbol)kvp.Value).Name
            }
        ).ToImmutableArray();

        // If we are not inheriting ISerializable, then we need to define some stuff
        if (!(isOverride || embedded))
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

        if (!embedded)
        {
            // Serial constructor
            source.GenerateSerialCtor(compilation, className, indent, isOverride);
            source.AppendLine();
        }

        if (version > 0)
        {
            for (var i = 0; i < migrations.Length; i++)
            {
                var migration = migrations[i];
                if (migration.Version < version)
                {
                    source.GenerateMigrationContentStruct(compilation, indent, migration, classSymbol);
                    source.AppendLine();
                }
            }
        }

        // Serialize Method
        source.GenerateSerializeMethod(
            compilation,
            indent,
            isOverride,
            encodedVersion,
            serializableFields,
            serializableProperties,
            serializableFieldSaveFlags
        );
        source.AppendLine();

        // Deserialize Method
        source.GenerateDeserializeMethod(
            compilation,
            classSymbol,
            indent,
            isOverride,
            version,
            encodedVersion,
            migrations,
            serializableFields,
            serializableProperties,
            parentFieldOrProperty,
            serializableFieldSaveFlags
        );

        // Serialize SaveFlag enum class
        if (serializableFieldSaveFlags.Count > 0)
        {
            source.AppendLine();
            source.GenerateEnumStart(
                "SaveFlag",
                $"{indent}    ",
                true,
                Accessibility.Private
            );

            source.GenerateEnumValue($"{indent}        ", true, "None", -1);
            int index = 0;
            foreach (var (order, _) in serializableFieldSaveFlags)
            {
                source.GenerateEnumValue($"{indent}        ", true, serializableProperties[order].Name, index++);
            }

            source.GenerateEnumEnd($"{indent}    ");
        }

        source.RecursiveGenerateClassEnd(classSymbol, ref indent);
        source.GenerateNamespaceEnd();

        if (migrationPath != null)
        {
            // Write the migration file
            var newMigration = new SerializableMetadata
            {
                Version = version,
                Type = classSymbol.ToDisplayString(),
                Properties = serializableProperties.Length > 0 ? serializableProperties : null
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

    private static void RecursiveGenerateClassStart(
        this StringBuilder source,
        INamedTypeSymbol classSymbol,
        ImmutableArray<ITypeSymbol> interfaces,
        ref string indent
    )
    {
        var containingSymbolList = new List<INamedTypeSymbol>();

        do
        {
            containingSymbolList.Add(classSymbol);
            classSymbol = classSymbol.ContainingSymbol as INamedTypeSymbol;
        } while (classSymbol != null);

        containingSymbolList.Reverse();

        for (var i = 0; i < containingSymbolList.Count; i++)
        {
            var symbol = containingSymbolList[i];
            source.GenerateClassStart(symbol, indent, i == containingSymbolList.Count - 1 ? interfaces : ImmutableArray<ITypeSymbol>.Empty);
            indent += "    ";
        }
    }

    private static void RecursiveGenerateClassEnd(this StringBuilder source, INamedTypeSymbol classSymbol, ref string indent)
    {
        do
        {
            indent = indent.Substring(0, indent.Length - 4);
            source.GenerateClassEnd(indent);

            classSymbol = classSymbol.ContainingSymbol as INamedTypeSymbol;
        } while (classSymbol != null);
    }
}