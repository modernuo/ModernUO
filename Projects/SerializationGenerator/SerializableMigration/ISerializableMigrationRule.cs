/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableMigrationRule.cs                                    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SerializableMigration
{
    public interface ISerializableMigrationRule
    {
        string RuleName { get; }

        bool GenerateRuleState(
            Compilation compilation,
            ISymbol symbol,
            ImmutableArray<AttributeData> attributes,
            ImmutableArray<INamedTypeSymbol> serializableTypes,
            ImmutableArray<INamedTypeSymbol> embeddedSerializableTypes,
            ISymbol? parentSymbol,
            out string[] ruleArguments
        );

        void GenerateDeserializationMethod(
            StringBuilder source,
            string indent,
            SerializableProperty property,
            string? parentReference
        );

        void GenerateSerializationMethod(
            StringBuilder source,
            string indent,
            SerializableProperty property
        );
    }
}
