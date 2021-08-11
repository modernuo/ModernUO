/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ListMigrationRule.cs                                            *
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
    public class ListMigrationRule : ISerializableMigrationRule
    {
        public string RuleName => nameof(ListMigrationRule);

        public bool GenerateRuleState(
            Compilation compilation,
            ISymbol symbol,
            ImmutableArray<AttributeData> attributes,
            ImmutableArray<INamedTypeSymbol> serializableTypes,
            ISymbol? parentSymbol,
            out string[] ruleArguments
        )
        {
            if (symbol is not INamedTypeSymbol namedTypeSymbol || !symbol.IsList(compilation))
            {
                ruleArguments = null;
                return false;
            }

            var listTypeSymbol = namedTypeSymbol.TypeArguments[0];

            var serializableListType = SerializableMigrationRulesEngine.GenerateSerializableProperty(
                compilation,
                "ListEntry",
                listTypeSymbol,
                0,
                attributes,
                serializableTypes,
                parentSymbol
            );

            var length = serializableListType.RuleArguments.Length;
            ruleArguments = new string[length + 2];
            ruleArguments[0] = listTypeSymbol.ToDisplayString();
            ruleArguments[1] = serializableListType.Rule;
            Array.Copy(serializableListType.RuleArguments, 0, ruleArguments, 2, length);

            return true;
        }

        public void GenerateDeserializationMethod(StringBuilder source, string indent, SerializableProperty property)
        {
            const string expectedRule = nameof(ListMigrationRule);
            var ruleName = property.Rule;
            if (expectedRule != ruleName)
            {
                throw new ArgumentException($"Invalid rule applied to property {ruleName}. Expecting {expectedRule}, but received {ruleName}.");
            }

            var ruleArguments = property.RuleArguments;
            var listElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[1]];
            var listElementRuleArguments = new string[ruleArguments.Length - 2];
            Array.Copy(ruleArguments, 2, listElementRuleArguments, 0, ruleArguments.Length - 2);

            var propertyName = property.Name;
            var propertyVarPrefix = $"{char.ToLower(propertyName[0])}{propertyName.Substring(1, propertyName.Length - 1)}";
            var propertyIndex = $"{propertyVarPrefix}Index";
            var propertyEntry = $"{propertyVarPrefix}Entry";
            var propertyCount = $"{propertyVarPrefix}Count";

            source.AppendLine($"{indent}{ruleArguments[0]} {propertyEntry};");
            source.AppendLine($"{indent}var {propertyCount} = reader.ReadInt();");
            source.AppendLine($"{indent}{propertyName} = new System.Collections.Generic.List<{ruleArguments[0]}>({propertyCount});");
            source.AppendLine($"{indent}for (var {propertyIndex} = 0; {propertyIndex} < {propertyCount}; {propertyIndex}++)");
            source.AppendLine($"{indent}{{");

            var serializableListElement = new SerializableProperty
            {
                Name = propertyEntry,
                Type = ruleArguments[0],
                Rule = listElementRule.RuleName,
                RuleArguments = listElementRuleArguments
            };

            listElementRule.GenerateDeserializationMethod(source, $"{indent}    ", serializableListElement);
            source.AppendLine($"{indent}    {propertyName}.Add({propertyEntry});");

            source.AppendLine($"{indent}}}");
        }

        public void GenerateSerializationMethod(StringBuilder source, string indent, SerializableProperty property)
        {
            const string expectedRule = nameof(ListMigrationRule);
            var ruleName = property.Rule;
            if (expectedRule != ruleName)
            {
                throw new ArgumentException($"Invalid rule applied to property {ruleName}. Expecting {expectedRule}, but received {ruleName}.");
            }

            var ruleArguments = property.RuleArguments;
            var listElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[1]];
            var listElementRuleArguments = new string[ruleArguments.Length - 2];
            Array.Copy(ruleArguments, 2, listElementRuleArguments, 0, ruleArguments.Length - 2);

            var propertyName = property.Name;
            var propertyVarPrefix = $"{char.ToLower(propertyName[0])}{propertyName.Substring(1, propertyName.Length - 1)}";
            var propertyEntry = $"{propertyVarPrefix}Entry";
            var propertyCount = $"{propertyVarPrefix}Count";
            source.AppendLine($"{indent}var {propertyCount} = {property.Name}?.Count ?? 0;");
            source.AppendLine($"{indent}writer.Write({propertyCount});");
            source.AppendLine($"{indent}if ({propertyCount} > 0)");
            source.AppendLine($"{indent}{{");
            source.AppendLine($"{indent}    foreach (var {propertyEntry} in {property.Name}!)");
            source.AppendLine($"{indent}    {{");

            var serializableListElement = new SerializableProperty
            {
                Name = propertyEntry,
                Type = ruleArguments[0],
                Rule = listElementRule.RuleName,
                RuleArguments = listElementRuleArguments
            };

            listElementRule.GenerateSerializationMethod(source, $"{indent}        ", serializableListElement);

            source.AppendLine($"{indent}    }}");
            source.AppendLine($"{indent}}}");
        }
    }
}
