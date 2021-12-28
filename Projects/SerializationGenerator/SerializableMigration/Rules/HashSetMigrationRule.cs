/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: HashSetMigrationRule.cs                                         *
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
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using SerializationGenerator;

namespace SerializableMigration;

public class HashSetMigrationRule : MigrationRule
{
    public override string RuleName => nameof(HashSetMigrationRule);

    public override bool GenerateRuleState(
        Compilation compilation,
        ISymbol symbol,
        ImmutableArray<AttributeData> attributes,
        ImmutableArray<INamedTypeSymbol> serializableTypes,
        ImmutableArray<INamedTypeSymbol> embeddedSerializableTypes,
        ISymbol? parentSymbol,
        out string[] ruleArguments
    )
    {
        if (symbol is not INamedTypeSymbol namedTypeSymbol || !symbol.IsHashSet(compilation))
        {
            ruleArguments = null;
            return false;
        }

        var setTypeSymbol = namedTypeSymbol.TypeArguments[0];

        var serializableSetType = SerializableMigrationRulesEngine.GenerateSerializableProperty(
            compilation,
            "SetEntry",
            setTypeSymbol,
            0,
            attributes,
            serializableTypes,
            embeddedSerializableTypes,
            parentSymbol,
            null
        );

        var extraOptions = "";
        if (attributes.Any(a => a.IsTidy(compilation)))
        {
            extraOptions += "@Tidy";
        }

        var length = serializableSetType.RuleArguments?.Length ?? 0;
        ruleArguments = new string[length + 3];
        ruleArguments[0] = extraOptions;
        ruleArguments[1] = setTypeSymbol.ToDisplayString();
        ruleArguments[2] = serializableSetType.Rule;

        if (length > 0)
        {
            Array.Copy(serializableSetType.RuleArguments, 0, ruleArguments, 3, length);
        }

        return true;
    }

    public override void GenerateDeserializationMethod(
        StringBuilder source, string indent, SerializableProperty property, string? parentReference, bool isMigration = false
    )
    {
        var expectedRule = RuleName;
        var ruleName = property.Rule;
        if (expectedRule != ruleName)
        {
            throw new ArgumentException($"Invalid rule applied to property {ruleName}. Expecting {expectedRule}, but received {ruleName}.");
        }

        var ruleArguments = property.RuleArguments;
        var hasExtraOptions = ruleArguments![0] == "" || ruleArguments[0].StartsWith("@", StringComparison.Ordinal);
        var argumentsOffset = hasExtraOptions ? 1 : 0;

        var setElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[1 + argumentsOffset]];
        var setElementRuleArguments = new string[ruleArguments.Length - 2 - argumentsOffset];
        Array.Copy(ruleArguments, 2 + argumentsOffset, setElementRuleArguments, 0, ruleArguments.Length - 2 - argumentsOffset);

        var propertyName = property.Name;
        var propertyVarPrefix = $"{char.ToLower(propertyName[0])}{propertyName.Substring(1, propertyName.Length - 1)}";
        var propertyIndex = $"{propertyVarPrefix}Index";
        var propertyEntry = $"{propertyVarPrefix}Entry";
        var propertyCount = $"{propertyVarPrefix}Count";

        source.AppendLine($"{indent}{ruleArguments[argumentsOffset]} {propertyEntry};");
        source.AppendLine($"{indent}var {propertyCount} = reader.ReadEncodedInt();");
        source.AppendLine($"{indent}{property.Name} = new System.Collections.Generic.HashSet<{ruleArguments[argumentsOffset]}>({propertyCount});");
        source.AppendLine($"{indent}for (var {propertyIndex} = 0; {propertyIndex} < {propertyCount}; {propertyIndex}++)");
        source.AppendLine($"{indent}{{");

        var serializableSetElement = new SerializableProperty
        {
            Name = propertyEntry,
            Type = ruleArguments[argumentsOffset],
            Rule = setElementRule.RuleName,
            RuleArguments = setElementRuleArguments
        };

        setElementRule.GenerateDeserializationMethod(source, $"{indent}    ", serializableSetElement, parentReference);
        source.AppendLine($"{indent}    {property.Name}.Add({propertyEntry});");

        source.AppendLine($"{indent}}}");
    }

    public override void GenerateSerializationMethod(StringBuilder source, string indent, SerializableProperty property)
    {
        var expectedRule = RuleName;
        var ruleName = property.Rule;
        if (expectedRule != ruleName)
        {
            throw new ArgumentException($"Invalid rule applied to property {ruleName}. Expecting {expectedRule}, but received {ruleName}.");
        }

        var ruleArguments = property.RuleArguments;
        var hasExtraOptions = ruleArguments![0] == "" || ruleArguments[0].StartsWith("@", StringComparison.Ordinal);
        var shouldTidy = hasExtraOptions && ruleArguments[0].Contains("@Tidy");
        var argumentsOffset = hasExtraOptions ? 1 : 0;

        var setElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[1 + argumentsOffset]];
        var setElementRuleArguments = new string[ruleArguments.Length - 2 - argumentsOffset];
        Array.Copy(ruleArguments, 2 + argumentsOffset, setElementRuleArguments, 0, ruleArguments.Length - 2 - argumentsOffset);

        var propertyName = property.Name;
        var propertyVarPrefix = $"{char.ToLower(propertyName[0])}{propertyName.Substring(1, propertyName.Length - 1)}";
        var propertyEntry = $"{propertyVarPrefix}Entry";
        var propertyCount = $"{propertyVarPrefix}Count";

        if (shouldTidy)
        {
            source.AppendLine($"{indent}{property.Name}?.Tidy();");
        }
        source.AppendLine($"{indent}var {propertyCount} = {property.Name}?.Count ?? 0;");
        source.AppendLine($"{indent}writer.WriteEncodedInt({propertyCount});");
        source.AppendLine($"{indent}if ({propertyCount} > 0)");
        source.AppendLine($"{indent}{{");
        source.AppendLine($"{indent}    foreach (var {propertyEntry} in {property.Name}!)");
        source.AppendLine($"{indent}    {{");

        var serializableSetElement = new SerializableProperty
        {
            Name = propertyEntry,
            Type = ruleArguments[argumentsOffset],
            Rule = setElementRule.RuleName,
            RuleArguments = setElementRuleArguments
        };

        setElementRule.GenerateSerializationMethod(source, $"{indent}        ", serializableSetElement);

        source.AppendLine($"{indent}    }}");
        source.AppendLine($"{indent}}}");
    }
}
