using Microsoft.CodeAnalysis;

namespace SerializationGenerator;

public record SerializableFieldSaveFlagMethods
{
    public IMethodSymbol? DetermineFieldShouldSerialize { get; init; }

    public IMethodSymbol? GetFieldDefaultValue { get; init; }
}