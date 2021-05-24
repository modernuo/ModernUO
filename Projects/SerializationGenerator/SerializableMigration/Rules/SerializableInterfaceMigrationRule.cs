/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableInterfaceMigrationRule.cs                           *
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
    public class SerializableInterfaceMigrationRule : ISerializableMigrationRule
    {
        static SerializableInterfaceMigrationRule()
        {
            var rule = new SerializableInterfaceMigrationRule();
            SerializableMigrationRulesEngine.Rules.Add(rule.RuleName, rule);
        }

        public string RuleName => nameof(SerializableInterfaceMigrationRule);

        public bool GenerateRuleState(
            Compilation compilation,
            ISymbol symbol,
            ImmutableArray<AttributeData> attributes,
            ImmutableArray<INamedTypeSymbol> serializableTypes,
            out string[] ruleArguments
        )
        {
            if (symbol is ITypeSymbol typeSymbol && typeSymbol.HasSerializableInterface(compilation, serializableTypes))
            {
                ruleArguments = new[] { symbol.ToDisplayString() };
                return true;
            }

            ruleArguments = null;
            return false;
        }

        public void GenerateDeserializationMethod(StringBuilder source, string indent, SerializableProperty property)
        {
            var propertyName = property.Name;

            if (property.Rule != nameof(PrimitiveTypeMigrationRule))
            {
                throw new ArgumentException($"Invalid rule applied to property {propertyName}.");
            }

            source.AppendLine($"{indent}{propertyName} = reader.ReadEntity<{property.RuleArguments[0]}>()");
        }

        public void GenerateSerializationMethod(StringBuilder source, string indent, SerializableProperty property)
        {
            var propertyName = property.Name;

            if (property.Rule != nameof(PrimitiveTypeMigrationRule))
            {
                throw new ArgumentException($"Invalid rule applied to property {propertyName}.");
            }

            source.AppendLine($"{indent}writer.Write({propertyName});");
        }
    }
}
