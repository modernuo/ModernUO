/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableMigrationRulesEngine.cs                             *
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
using Microsoft.CodeAnalysis;

namespace SerializationGenerator
{
    public static class SerializableMigrationRulesEngine
    {
        private static readonly HashSet<ISerializableMigrationRule> _rules = new();

        public static void RegisterRule(ISerializableMigrationRule rule) => _rules.Add(rule);

        public static SerializableProperty GenerateSerializableProperty(
            Compilation compilation,
            string propertyName,
            ISymbol symbol,
            ImmutableArray<AttributeData> attributes,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {
            string propertyType = symbol switch
            {
                IFieldSymbol fieldSymbol => fieldSymbol.Type.ToDisplayString(),
                IPropertySymbol propertySymbol => propertySymbol.Type.ToDisplayString(),
                _ => throw new ArgumentException($"Invalid symbol type provided for {propertyName}")
            };

            foreach (var rule in _rules)
            {
                if (rule.GenerateRuleState(
                    compilation,
                    symbol,
                    attributes,
                    serializableTypes,
                    out var ruleArguments
                ))
                {
                    return new SerializableProperty
                    {
                        Name = propertyName,
                        Type = propertyType,
                        Rule = rule.RuleName,
                        RuleArguments = ruleArguments
                    };
                }
            }

            throw new Exception($"No rule found for property {propertyName} of type {propertyType}");
        }
    }
}
