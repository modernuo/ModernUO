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
using System.Text;
using Microsoft.CodeAnalysis;

namespace SerializationGenerator
{
    public class HashSetMigrationRule : ISerializableMigrationRule
    {
        static HashSetMigrationRule()
        {
            var rule = new HashSetMigrationRule();
            SerializableMigrationRulesEngine.Rules.Add(rule.RuleName, rule);
        }

        public string RuleName => nameof(HashSetMigrationRule);

        public bool GenerateRuleState(
            Compilation compilation,
            ISymbol symbol,
            ImmutableArray<AttributeData> attributes,
            ImmutableArray<INamedTypeSymbol> serializableTypes,
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
                attributes,
                serializableTypes
            );

            var length = serializableSetType.RuleArguments.Length;
            ruleArguments = new string[length + 2];
            ruleArguments[0] = setTypeSymbol.ToDisplayString();
            ruleArguments[1] = serializableSetType.Rule;
            Array.Copy(serializableSetType.RuleArguments, 0, ruleArguments, 2, length);

            return true;
        }

        public void GenerateDeserializationMethod(StringBuilder source, string indent, SerializableProperty property)
        {
            var ruleArguments = property.RuleArguments;
            var setElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[1]];
            var setElementRuleArguments = new string[ruleArguments.Length - 2];
            Array.Copy(ruleArguments, 2, setElementRuleArguments, 0, ruleArguments.Length - 2);

            var propertyIndex = $"{property.Name}Index";
            var propertyEntry = $"{property.Name}Entry";
            source.AppendLine($"{indent}{ruleArguments[0]} {propertyEntry};");
            source.AppendLine($"{indent}{property.Name} = new {SerializableEntityGeneration.HASHSET_CLASS}<{ruleArguments[0]}>(reader.ReadEncodedInt());");
            source.AppendLine($"{indent}for (var {propertyIndex} = 0; i < {property.Name}.Count; {propertyIndex}++)");
            source.AppendLine($"{indent}{{");

            var serializableSetElement = new SerializableProperty
            {
                Name = propertyEntry,
                Type = ruleArguments[0],
                Rule = setElementRule.RuleName,
                RuleArguments = setElementRuleArguments
            };

            setElementRule.GenerateDeserializationMethod(source, $"{indent}    ", serializableSetElement);
            source.AppendLine($"{indent}    {property.Name}.Add({propertyEntry})");

            source.AppendLine($"{indent}}}");
        }

        public void GenerateSerializationMethod(StringBuilder source, string indent, SerializableProperty property)
        {
            var ruleArguments = property.RuleArguments;
            var setElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[1]];
            var setElementRuleArguments = new string[ruleArguments.Length - 2];
            Array.Copy(ruleArguments, 2, setElementRuleArguments, 0, ruleArguments.Length - 2);

            var propertyEntry = $"{property.Name}Entry";
            source.AppendLine($"{indent}writer.WriteEncodedInt({property.Name}.Count);");
            source.AppendLine($"{indent}foreach (var {propertyEntry} in {property.Name})");
            source.AppendLine($"{indent}{{");

            var serializableSetElement = new SerializableProperty
            {
                Name = propertyEntry,
                Type = ruleArguments[0],
                Rule = setElementRule.RuleName,
                RuleArguments = setElementRuleArguments
            };

            setElementRule.GenerateSerializationMethod(source, $"{indent}    ", serializableSetElement);

            source.AppendLine($"{indent}}}");
        }
    }
}
