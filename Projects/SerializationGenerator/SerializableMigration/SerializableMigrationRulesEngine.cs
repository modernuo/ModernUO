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
using SerializationGenerator;

namespace SerializableMigration
{
    public static class SerializableMigrationRulesEngine
    {
        public static readonly Dictionary<string, ISerializableMigrationRule> Rules = new();

        static SerializableMigrationRulesEngine()
        {
            var rules = new ISerializableMigrationRule[]
            {
                new EnumMigrationRule(),
                new ListMigrationRule(),
                new ArrayMigrationRule(),
                new HashSetMigrationRule(),
                new KeyValuePairMigrationRule(),
                new PrimitiveTypeMigrationRule(),
                new PrimitiveUOTypeMigrationRule(),
                new SerializableInterfaceMigrationRule(),
                new SerializationMethodSignatureMigrationRule(),
            };

            foreach (var rule in rules)
            {
                Rules.Add(rule.RuleName, rule);
            }
        }

        public static SerializableProperty? GenerateSerializableProperty(
            Compilation compilation,
            ISymbol fieldOrPropertySymbol,
            int order,
            ImmutableArray<AttributeData> attributes,
            ImmutableArray<INamedTypeSymbol> serializableTypes,
            ISymbol? parentSymbol = default
        )
        {
            string propertyName;
            ITypeSymbol propertyType;

            if (fieldOrPropertySymbol is IFieldSymbol fieldSymbol)
            {
                propertyName = fieldSymbol.GetPropertyName();
                propertyType = fieldSymbol.Type;
            }
            else if (fieldOrPropertySymbol is IPropertySymbol propertySymbol)
            {
                propertyName = fieldOrPropertySymbol.Name;
                propertyType = propertySymbol.Type;
            }
            else
            {
                return null;
            }

            return GenerateSerializableProperty(
                compilation,
                propertyName,
                propertyType,
                order,
                attributes,
                serializableTypes,
                parentSymbol
            );
        }

        public static SerializableProperty GenerateSerializableProperty(
            Compilation compilation,
            string propertyName,
            ISymbol propertyType,
            int order,
            ImmutableArray<AttributeData> attributes,
            ImmutableArray<INamedTypeSymbol> serializableTypes,
            ISymbol? parentSymbol = default
        )
        {
            foreach (var rule in Rules.Values)
            {
                if (rule.GenerateRuleState(
                    compilation,
                    propertyType,
                    attributes,
                    serializableTypes,
                    parentSymbol,
                    out var ruleArguments
                ))
                {
                    return new SerializableProperty
                    {
                        Name = propertyName,
                        Type = propertyType.ToDisplayString(),
                        Order = order,
                        Rule = rule.RuleName,
                        RuleArguments = ruleArguments
                    };
                }
            }

            throw new Exception($"No rule found for property {propertyName} of type {propertyType} ({Rules.Count})");
        }
    }
}
