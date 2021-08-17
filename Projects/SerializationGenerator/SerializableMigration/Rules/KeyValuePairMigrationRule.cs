/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: KeyValuePairMigrationRule.cs                                    *
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
using System.Text;
using Microsoft.CodeAnalysis;
using SerializationGenerator;

namespace SerializableMigration
{
    public class KeyValuePairMigrationRule : ISerializableMigrationRule
    {
        public string RuleName => nameof(KeyValuePairMigrationRule);

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
            if (symbol is not INamedTypeSymbol namedTypeSymbol || !symbol.IsKeyValuePair(compilation))
            {
                ruleArguments = null;
                return false;
            }

            var typeArguments = namedTypeSymbol.TypeArguments;

            var keySerializedProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
                compilation,
                "key",
                typeArguments[0],
                0,
                attributes,
                serializableTypes,
                embeddedSerializableTypes,
                parentSymbol,
                null
            );

            var valueSerializedProperty = SerializableMigrationRulesEngine.GenerateSerializableProperty(
                compilation,
                "value",
                typeArguments[1],
                1,
                attributes,
                serializableTypes,
                embeddedSerializableTypes,
                parentSymbol,
                null
            );

            var keyArgumentsLength = keySerializedProperty.RuleArguments?.Length ?? 0;
            var valueArgumentsLength = valueSerializedProperty.RuleArguments?.Length ?? 0;
            var index = 0;

            // Key
            ruleArguments = new string[5 + keyArgumentsLength + valueArgumentsLength];
            ruleArguments[index++] = typeArguments[0].ToDisplayString();
            ruleArguments[index++] = keySerializedProperty.Rule;
            ruleArguments[index++] = keyArgumentsLength.ToString();
            if (keyArgumentsLength > 0)
            {
                Array.Copy(keySerializedProperty.RuleArguments!, 0, ruleArguments, index, keyArgumentsLength);
            }

            // Value
            ruleArguments[index++] = typeArguments[1].ToDisplayString();
            ruleArguments[index++] = valueSerializedProperty.Rule;

            if (valueArgumentsLength > 0)
            {
                Array.Copy(valueSerializedProperty.RuleArguments!, 0, ruleArguments, index, valueArgumentsLength);
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
            var keyType = ruleArguments![0];
            var keyRule = SerializableMigrationRulesEngine.Rules[ruleArguments[1]];
            var keyRuleArguments = new string[int.Parse(ruleArguments[2])];
            Array.Copy(ruleArguments, 3, keyRuleArguments, 0, keyRuleArguments.Length);

            var serializableKeyProperty = new SerializableProperty
            {
                Name = "key",
                Type = keyType,
                Rule = keyRule.RuleName,
                RuleArguments = keyRuleArguments
            };

            keyRule.GenerateDeserializationMethod(
                source,
                indent,
                serializableKeyProperty,
                parentReference
            );

            var valueIndex = 3 + keyRuleArguments.Length;
            var valueType = ruleArguments[valueIndex++];
            var valueRule = SerializableMigrationRulesEngine.Rules[ruleArguments[valueIndex++]];
            var valueRuleArguments = new string[ruleArguments.Length - valueIndex];
            Array.Copy(ruleArguments, valueIndex, valueRuleArguments, 0, valueRuleArguments.Length);

            var serializableValueProperty = new SerializableProperty
            {
                Name = "value",
                Type = valueType,
                Rule = valueRule.RuleName,
                RuleArguments = valueRuleArguments
            };

            keyRule.GenerateDeserializationMethod(
                source,
                indent,
                serializableValueProperty,
                parentReference
            );

            source.AppendLine(
                $"{indent}{property.Name} = new {SymbolMetadata.KEYVALUEPAIR_STRUCT}<{keyType}, {valueType}>(key, value);"
            );
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
            var keyType = ruleArguments![0];
            var keyRule = SerializableMigrationRulesEngine.Rules[ruleArguments[1]];
            var keyRuleArguments = new string[int.Parse(ruleArguments[2])];
            Array.Copy(ruleArguments, 3, keyRuleArguments, 0, keyRuleArguments.Length);

            var serializableKeyProperty = new SerializableProperty
            {
                Name = $"{property.Name}.Key",
                Type = keyType,
                Rule = keyRule.RuleName,
                RuleArguments = keyRuleArguments
            };

            keyRule.GenerateSerializationMethod(
                source,
                indent,
                serializableKeyProperty
            );

            var valueIndex = 3 + keyRuleArguments.Length;
            var valueType = ruleArguments[valueIndex++];
            var valueRule = SerializableMigrationRulesEngine.Rules[ruleArguments[valueIndex++]];
            var valueRuleArguments = new string[ruleArguments.Length - valueIndex];
            Array.Copy(ruleArguments, valueIndex, valueRuleArguments, 0, valueRuleArguments.Length);

            var serializableValueProperty = new SerializableProperty
            {
                Name = $"{property.Name}.Value",
                Type = valueType,
                Rule = valueRule.RuleName,
                RuleArguments = valueRuleArguments
            };

            keyRule.GenerateSerializationMethod(
                source,
                indent,
                serializableValueProperty
            );
        }
    }
}
