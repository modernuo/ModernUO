/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PrimitiveUOTypeMigrationRule.cs                                 *
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
    public class PrimitiveUOTypeMigrationRule : ISerializableMigrationRule
    {
        static PrimitiveUOTypeMigrationRule()
        {
            var rule = new PrimitiveUOTypeMigrationRule();
            SerializableMigrationRulesEngine.Rules.Add(rule.RuleName, rule);
        }

        public string RuleName => nameof(PrimitiveUOTypeMigrationRule);

        public bool GenerateRuleState(
            Compilation compilation,
            ISymbol symbol,
            ImmutableArray<AttributeData> attributes,
            ImmutableArray<INamedTypeSymbol> serializableTypes,
            out string[] ruleArguments
        )
        {
            ruleArguments = symbol switch
            {
                _ when symbol.IsPoint2D(compilation)     => new[] { "Point2D" },
                _ when symbol.IsPoint3D(compilation)     => new[] { "Point3D" },
                _ when symbol.IsRectangle2D(compilation) => new[] { "Rect2D" },
                _ when symbol.IsRectangle3D(compilation) => new[] { "Rect3D" },
                _ when symbol.IsRace(compilation)        => new[] { "Race" },
                _ when symbol.IsMap(compilation)         => new[] { "Map" },
                _ => null
            };

            return ruleArguments != null;
        }

        public void GenerateDeserializationMethod(StringBuilder source, string indent, SerializableProperty property)
        {
            var propertyName = property.Name;

            if (property.Rule != nameof(PrimitiveTypeMigrationRule))
            {
                throw new ArgumentException($"Invalid rule applied to property {propertyName}.");
            }

            source.AppendLine($"{indent}{propertyName} = reader.Read{property.RuleArguments[0]}()");
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
