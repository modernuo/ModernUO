/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DictionaryMigrationRule.cs                                      *
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

public class DictionaryMigrationRule : MigrationRule
{
    public override string RuleName => nameof(DictionaryMigrationRule);

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
        if (symbol is not INamedTypeSymbol namedTypeSymbol || !symbol.IsDictionary(compilation))
        {
            ruleArguments = null;
            return false;
        }

        var keySymbolType = namedTypeSymbol.TypeArguments[0];

        var serializableKeyProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
            compilation,
            "KeyEntry",
            keySymbolType,
            0,
            attributes,
            serializableTypes,
            embeddedSerializableTypes,
            parentSymbol,
            null
        );

        var valueSymbolType = namedTypeSymbol.TypeArguments[1];

        var serializableValueProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
            compilation,
            "ValueEntry",
            valueSymbolType,
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

        var keyArgumentsLength = serializableKeyProperty.RuleArguments?.Length ?? 0;
        var valueArgumentsLength = serializableValueProperty.RuleArguments?.Length ?? 0;
        var index = 0;

        ruleArguments = new string[7 + keyArgumentsLength + valueArgumentsLength];
        ruleArguments[index++] = extraOptions;
        ruleArguments[index++] = keySymbolType.ToDisplayString();
        ruleArguments[index++] = serializableKeyProperty.Rule;
        ruleArguments[index++] = keyArgumentsLength.ToString();

        if (keyArgumentsLength > 0)
        {
            Array.Copy(serializableKeyProperty.RuleArguments!, 0, ruleArguments, index, keyArgumentsLength);
            index += keyArgumentsLength;
        }

        ruleArguments[index++] = valueSymbolType.ToDisplayString();
        ruleArguments[index++] = serializableValueProperty.Rule;
        ruleArguments[index++] = valueArgumentsLength.ToString();

        if (valueArgumentsLength > 0)
        {
            Array.Copy(serializableValueProperty.RuleArguments!, 0, ruleArguments, index, valueArgumentsLength);
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
        var index = 1;
        var keyType = ruleArguments![index++];

        var keyElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[index++]];
        var keyRuleArguments = new string[int.Parse(ruleArguments[index++])];

        if (keyRuleArguments.Length > 0)
        {
            Array.Copy(ruleArguments, index, keyRuleArguments, 0, keyRuleArguments.Length);
            index += keyRuleArguments.Length;
        }

        var valueType = ruleArguments[index++];
        var valueElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[index++]];
        var valueRuleArguments = new string[int.Parse(ruleArguments[index++])];

        if (valueRuleArguments.Length > 0)
        {
            Array.Copy(ruleArguments, index, valueRuleArguments, 0, valueRuleArguments.Length);
        }

        var propertyName = property.Name;
        var propertyVarPrefix = $"{char.ToLower(propertyName[0])}{propertyName.Substring(1, propertyName.Length - 1)}";
        var propertyIndex = $"{propertyVarPrefix}Index";
        var propertyKeyEntry = $"{propertyVarPrefix}Key";
        var propertyValueEntry = $"{propertyVarPrefix}Value";
        var propertyCount = $"{propertyVarPrefix}Count";

        source.AppendLine($"{indent}{ruleArguments[1]} {propertyKeyEntry};");
        source.AppendLine($"{indent}{valueType} {propertyValueEntry};");
        source.AppendLine($"{indent}var {propertyCount} = reader.ReadEncodedInt();");
        source.AppendLine($"{indent}{propertyName} = new System.Collections.Generic.Dictionary<{keyType}, {valueType}>({propertyCount});");
        source.AppendLine($"{indent}for (var {propertyIndex} = 0; {propertyIndex} < {propertyCount}; {propertyIndex}++)");
        source.AppendLine($"{indent}{{");

        var serializableKeyElement = new SerializableProperty
        {
            Name = propertyKeyEntry,
            Type = keyType,
            Rule = keyElementRule.RuleName,
            RuleArguments = keyRuleArguments
        };

        keyElementRule.GenerateDeserializationMethod(source, $"{indent}    ", serializableKeyElement, parentReference);

        var serializableValueElement = new SerializableProperty
        {
            Name = propertyValueEntry,
            Type = valueType,
            Rule = valueElementRule.RuleName,
            RuleArguments = valueRuleArguments
        };

        valueElementRule.GenerateDeserializationMethod(source, $"{indent}    ", serializableValueElement, parentReference);
        source.AppendLine($"{indent}    {propertyName}.Add({propertyKeyEntry}, {propertyValueEntry});");

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
        var index = 0;
        var shouldTidy = ruleArguments![index++].Contains("@Tidy");
        var keyType = ruleArguments![index++];

        var keyElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments![index++]];
        var keyRuleArguments = new string[int.Parse(ruleArguments[index++])];

        if (keyRuleArguments.Length > 0)
        {
            Array.Copy(ruleArguments, index, keyRuleArguments, 0, keyRuleArguments.Length);
            index += keyRuleArguments.Length;
        }

        var valueType = ruleArguments[index++];
        var valueElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[index++]];
        var valueRuleArguments = new string[int.Parse(ruleArguments[index++])];

        if (valueRuleArguments.Length > 0)
        {
            Array.Copy(ruleArguments, index, valueRuleArguments, 0, valueRuleArguments.Length);
        }

        var propertyName = property.Name;
        var propertyVarPrefix = $"{char.ToLower(propertyName[0])}{propertyName.Substring(1, propertyName.Length - 1)}";
        var propertyKeyEntry = $"{propertyVarPrefix}Key";
        var propertyValueEntry = $"{propertyVarPrefix}Value";
        var propertyCount = $"{propertyVarPrefix}Count";

        if (shouldTidy)
        {
            source.AppendLine($"{indent}{property.Name}?.Tidy();");
        }
        source.AppendLine($"{indent}var {propertyCount} = {property.Name}?.Count ?? 0;");
        source.AppendLine($"{indent}writer.WriteEncodedInt({propertyCount});");
        source.AppendLine($"{indent}if ({propertyCount} > 0)");
        source.AppendLine($"{indent}{{");
        source.AppendLine($"{indent}    foreach (var ({propertyKeyEntry}, {propertyValueEntry}) in {property.Name}!)");
        source.AppendLine($"{indent}    {{");

        var serializableKeyElement = new SerializableProperty
        {
            Name = propertyKeyEntry,
            Type = keyType,
            Rule = keyElementRule.RuleName,
            RuleArguments = keyRuleArguments
        };

        keyElementRule.GenerateSerializationMethod(source, $"{indent}        ", serializableKeyElement);

        var serializableValueElement = new SerializableProperty
        {
            Name = propertyValueEntry,
            Type = valueType,
            Rule = valueElementRule.RuleName,
            RuleArguments = valueRuleArguments
        };

        valueElementRule.GenerateSerializationMethod(source, $"{indent}        ", serializableValueElement);

        source.AppendLine($"{indent}    }}");
        source.AppendLine($"{indent}}}");
    }
}
