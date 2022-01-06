using System;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using SerializationGenerator;

namespace SerializableMigration;

public abstract class MigrationRule : ISerializableMigrationRule
{
    public abstract string RuleName { get; }

    public virtual void GenerateMigrationProperty(
        StringBuilder source, Compilation compilation, string indent, SerializableProperty serializableProperty
    )
    {
        var propertyType = serializableProperty.Type;
        var type = compilation.GetTypeByMetadataName(propertyType)?.IsValueType == true
                   || SymbolMetadata.IsPrimitiveFromTypeDisplayString(propertyType) && propertyType != "bool"
            ? $"{propertyType}{(serializableProperty.UsesSaveFlag == true ? "?" : "")}" : propertyType;

        source.AppendLine($"{indent}internal readonly {type} {serializableProperty.Name};");
    }

    public abstract bool GenerateRuleState(
        Compilation compilation, ISymbol symbol, ImmutableArray<AttributeData> attributes,
        ImmutableArray<INamedTypeSymbol> serializableTypes,
        ImmutableArray<INamedTypeSymbol> embeddedSerializableTypes, ISymbol? parentSymbol, out string[] ruleArguments
    );

    public abstract void GenerateDeserializationMethod(
        StringBuilder source, string indent, SerializableProperty property, string? parentReference, bool isMigration = false
    );

    public abstract void GenerateSerializationMethod(StringBuilder source, string indent, SerializableProperty property);
}
