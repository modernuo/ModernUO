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
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using SerializationGenerator;

namespace SerializableMigration
{
    public class DictionaryMigrationRule : ISerializableMigrationRule
    {
        private const string KEY_VALUE_PAIR_DELIMITER = "----";
        public string RuleName => nameof(DictionaryMigrationRule);

        public bool GenerateRuleState(
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

            var keyPropertyLength = serializableKeyProperty.RuleArguments?.Length ?? 0;
            var valuePropertyLength = serializableValueProperty.RuleArguments?.Length ?? 0;
            ruleArguments = new string[keyPropertyLength + valuePropertyLength + 6];
            ruleArguments[0] = extraOptions;
            ruleArguments[1] = keySymbolType.ToDisplayString();
            ruleArguments[2] = serializableKeyProperty.Rule;

            if (keyPropertyLength > 0)
            {
                Array.Copy(serializableKeyProperty.RuleArguments!, 0, ruleArguments, 3, keyPropertyLength);
            }

            ruleArguments[3 + keyPropertyLength] = KEY_VALUE_PAIR_DELIMITER;
            ruleArguments[4 + keyPropertyLength] = valueSymbolType.ToDisplayString();
            ruleArguments[5 + keyPropertyLength] = serializableValueProperty.Rule;

            if (valuePropertyLength > 0)
            {
                Array.Copy(serializableValueProperty.RuleArguments!, 0, ruleArguments, 6 + keyPropertyLength, valuePropertyLength);
            }

            return true;
        }

        public void GenerateDeserializationMethod(StringBuilder source, string indent, SerializableProperty property, string? parentReference)
        {
            var expectedRule = RuleName;
            var ruleName = property.Rule;
            if (expectedRule != ruleName)
            {
                throw new ArgumentException($"Invalid rule applied to property {ruleName}. Expecting {expectedRule}, but received {ruleName}.");
            }

            var ruleArguments = property.RuleArguments;

            var keyElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments![2]];
            var valueRuleIndex = Array.IndexOf(ruleArguments, KEY_VALUE_PAIR_DELIMITER, 4);
            if (valueRuleIndex == -1)
            {
                throw new InvalidDataException($"Cannot find key-value delimiter in arguments for {property.Name}");
            }

            var keyRuleArguments = new string[valueRuleIndex - 3];
            Array.Copy(ruleArguments, 3, keyRuleArguments, 0, keyRuleArguments.Length);

            var valueElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[valueRuleIndex + 2]];
            var valueRuleArguments = new string[ruleArguments.Length - valueRuleIndex - 2];
            Array.Copy(ruleArguments, 2 + valueRuleIndex, valueRuleArguments, 0, valueRuleArguments.Length);

            var propertyName = property.Name;
            var propertyVarPrefix = $"{char.ToLower(propertyName[0])}{propertyName.Substring(1, propertyName.Length - 1)}";
            var propertyIndex = $"{propertyVarPrefix}Index";
            var propertyKeyEntry = $"{propertyVarPrefix}Key";
            var propertyValueEntry = $"{propertyVarPrefix}Value";
            var propertyCount = $"{propertyVarPrefix}Count";

            source.AppendLine($"{indent}{ruleArguments[1]} {propertyKeyEntry};");
            source.AppendLine($"{indent}{ruleArguments[valueRuleIndex + 1]} {propertyValueEntry};");
            source.AppendLine($"{indent}var {propertyCount} = reader.ReadEncodedInt();");
            source.AppendLine($"{indent}{propertyName} = new System.Collections.Generic.Dictionary<{ruleArguments[1]}, {ruleArguments[valueRuleIndex + 1]}>({propertyCount});");
            source.AppendLine($"{indent}for (var {propertyIndex} = 0; {propertyIndex} < {propertyCount}; {propertyIndex}++)");
            source.AppendLine($"{indent}{{");

            var serializableKeyElement = new SerializableProperty
            {
                Name = propertyKeyEntry,
                Type = ruleArguments[1],
                Rule = keyElementRule.RuleName,
                RuleArguments = keyRuleArguments
            };

            keyElementRule.GenerateDeserializationMethod(source, $"{indent}    ", serializableKeyElement, parentReference);

            var serializableValueElement = new SerializableProperty
            {
                Name = propertyValueEntry,
                Type = ruleArguments[valueRuleIndex + 1],
                Rule = valueElementRule.RuleName,
                RuleArguments = valueRuleArguments
            };

            valueElementRule.GenerateDeserializationMethod(source, $"{indent}    ", serializableValueElement, parentReference);
            source.AppendLine($"{indent}    {propertyName}.Add({propertyKeyEntry}, {propertyValueEntry});");

            source.AppendLine($"{indent}}}");
        }

        public void GenerateSerializationMethod(StringBuilder source, string indent, SerializableProperty property)
        {
            var expectedRule = RuleName;
            var ruleName = property.Rule;
            if (expectedRule != ruleName)
            {
                throw new ArgumentException($"Invalid rule applied to property {ruleName}. Expecting {expectedRule}, but received {ruleName}.");
            }

            var ruleArguments = property.RuleArguments;
            var shouldTidy = ruleArguments![0].Contains("@Tidy");

            var keyElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[2]];
            var valueRuleIndex = Array.IndexOf(ruleArguments, KEY_VALUE_PAIR_DELIMITER, 3);
            if (valueRuleIndex == -1)
            {
                throw new InvalidDataException($"Cannot find key-value delimiter in arguments for {property.Name}");
            }

            var keyRuleArguments = new string[valueRuleIndex - 3];
            Array.Copy(ruleArguments, 3, keyRuleArguments, 0, keyRuleArguments.Length);

            var valueElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[valueRuleIndex + 2]];
            var valueRuleArguments = new string[ruleArguments.Length - valueRuleIndex - 2];
            Array.Copy(ruleArguments, 2 + valueRuleIndex, valueRuleArguments, 0, valueRuleArguments.Length);

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
                Type = ruleArguments[1],
                Rule = keyElementRule.RuleName,
                RuleArguments = keyRuleArguments
            };

            keyElementRule.GenerateSerializationMethod(source, $"{indent}        ", serializableKeyElement);

            var serializableValueElement = new SerializableProperty
            {
                Name = propertyValueEntry,
                Type = ruleArguments[valueRuleIndex + 1],
                Rule = valueElementRule.RuleName,
                RuleArguments = valueRuleArguments
            };

            keyElementRule.GenerateSerializationMethod(source, $"{indent}        ", serializableValueElement);

            source.AppendLine($"{indent}    }}");
            source.AppendLine($"{indent}}}");
        }
    }
}
