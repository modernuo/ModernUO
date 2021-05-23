/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ArrayMigrationRule.cs                                           *
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
    public class ArrayMigrationRule : ISerializableMigrationRule
    {
        public string RuleName => "ArrayMigrationRule";

        public bool GenerateRuleState(
            Compilation compilation,
            ISymbol symbol,
            ImmutableArray<AttributeData> attributes,
            ImmutableArray<INamedTypeSymbol> serializableTypes,
            out string[] ruleArguments
        )
        {
            if (symbol is not IArrayTypeSymbol arrayTypeSymbol)
            {
                ruleArguments = null;
                return false;
            }

            var serializableArrayType = SerializableMigrationRulesEngine.GenerateSerializableProperty(
                compilation,
                "ArrayEntry",
                arrayTypeSymbol.ElementType,
                ImmutableArray<AttributeData>.Empty,
                serializableTypes
            );

            var length = serializableArrayType.RuleArguments.Length;
            ruleArguments = new string[length + 2];
            ruleArguments[0] = arrayTypeSymbol.ElementType.ToDisplayString();
            ruleArguments[1] = serializableArrayType.Rule;
            Array.Copy(serializableArrayType.RuleArguments, 0, ruleArguments, 2, length);

            return true;
        }

        public void GenerateDeserializationMethod(StringBuilder source, string indent, SerializableProperty property)
        {
            var ruleArguments = property.RuleArguments;
            var arrayElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[1]];
            var arrayElementRuleArguments = new string[ruleArguments.Length - 2];
            Array.Copy(ruleArguments, 2, arrayElementRuleArguments, 0, ruleArguments.Length - 2);

            source.AppendLine($"{indent}{property.Name} = new {ruleArguments[0]}[reader.ReadEncodedInt()];");
            source.AppendLine($"{indent}writer.WriteEncodedInt({property.Name}.Length);");
            source.AppendLine($"{indent}for (var i = 0; i < {property.Name}.Length; i++)");
            source.AppendLine($"{indent}{{");

            var serializableArrayElement = new SerializableProperty
            {
                Name = $"{property.Name}[i]",
                Type = ruleArguments[0],
                Rule = arrayElementRule.RuleName,
                RuleArguments = arrayElementRuleArguments
            };

            arrayElementRule.GenerateDeserializationMethod(source, $"{indent}    ", serializableArrayElement);

            source.AppendLine($"{indent}}}");
        }

        public void GenerateSerializationMethod(StringBuilder source, string indent, SerializableProperty property)
        {
            var ruleArguments = property.RuleArguments;
            var arrayElementRule = SerializableMigrationRulesEngine.Rules[ruleArguments[1]];
            var arrayElementRuleArguments = new string[ruleArguments.Length - 2];
            Array.Copy(ruleArguments, 2, arrayElementRuleArguments, 0, ruleArguments.Length - 2);

            source.AppendLine($"{indent}writer.WriteEncodedInt({property.Name}.Length);");
            source.AppendLine($"{indent}for (var i = 0; i < {property.Name}.Length; i++)");
            source.AppendLine($"{indent}{{");

            var serializableArrayElement = new SerializableProperty
            {
                Name = $"{property.Name}[i]",
                Type = ruleArguments[0],
                Rule = arrayElementRule.RuleName,
                RuleArguments = arrayElementRuleArguments
            };

            arrayElementRule.GenerateSerializationMethod(source, $"{indent}    ", serializableArrayElement);

            source.AppendLine($"{indent}}}");
        }
    }
}
