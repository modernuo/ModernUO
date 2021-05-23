/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PrimitiveTypeMigrationRule.cs                                   *
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
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SerializationGenerator
{
    public class PrimitiveTypeMigrationRule : ISerializableMigrationRule
    {
        public string RuleName => nameof(PrimitiveTypeMigrationRule);

        public bool GenerateRuleState(
            Compilation compilation,
            ISymbol symbol,
            ImmutableArray<AttributeData> attributes,
            ImmutableArray<INamedTypeSymbol> serializableTypes,
            out string[] ruleArguments
        )
        {
            if (symbol.IsIpAddress(compilation))
            {
                ruleArguments = new[] { "IPAddress" };
                return true;
            }

            if (symbol is not ITypeSymbol typedSymbol)
            {
                ruleArguments = null;
                return false;
            }

            if (
                typedSymbol.SpecialType is
                    SpecialType.System_Boolean or
                    SpecialType.System_SByte or
                    SpecialType.System_Int16 or
                    SpecialType.System_Int32 or
                    SpecialType.System_Int64 or
                    SpecialType.System_Byte or
                    SpecialType.System_UInt16 or
                    SpecialType.System_UInt32 or
                    SpecialType.System_UInt64 or
                    SpecialType.System_Single or
                    SpecialType.System_Double or
                    SpecialType.System_String or
                    SpecialType.System_Decimal or
                    SpecialType.System_DateTime
            )
            {
                ruleArguments = new[] { typedSymbol.SpecialType.ToString() };
                return true;
            }

            if (typedSymbol.SpecialType == SpecialType.System_DateTime)
            {
                ruleArguments = attributes.Any(a => a.IsDeltaDateTime(compilation))
                    ? new[] { typedSymbol.SpecialType.ToString(), "DeltaTime" }
                    : new[] { typedSymbol.SpecialType.ToString() };

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

            var ruleType = property.RuleArguments[0];
            string readMethod;

            if (ruleType == "IPAddress")
            {
                readMethod = "ReadIPAddress";
            }
            else
            {
                if (!Enum.TryParse<SpecialType>(ruleType, out var specialType))
                {
                    throw new ArgumentException($"Invalid rule state for property {propertyName} ({ruleType})");
                }

                readMethod = specialType switch
                {
                    SpecialType.System_Boolean => "ReadBool",
                    SpecialType.System_SByte   => "ReadSByte",
                    SpecialType.System_Int16   => "ReadShort",
                    SpecialType.System_Int32   => "ReadInt",
                    SpecialType.System_Int64   => "ReadLong",
                    SpecialType.System_Byte    => "ReadByte",
                    SpecialType.System_UInt16  => "ReadUShort",
                    SpecialType.System_UInt32  => "ReadUInt",
                    SpecialType.System_UInt64  => "ReadULong",
                    SpecialType.System_Single  => "ReadFloat",
                    SpecialType.System_Double  => "ReadDouble",
                    SpecialType.System_String  => "ReadString",
                    SpecialType.System_Decimal => "ReadDecimal",
                    SpecialType.System_DateTime => property.RuleArguments.Length >= 2 &&
                                                   property.RuleArguments[1] == "DeltaTime" ?
                        "ReadDeltaTime" :
                        "ReadDateTime"
                };
            }

            source.AppendLine($"{indent}{propertyName} = reader.{readMethod}()");
        }

        public void GenerateSerializationMethod(StringBuilder source, string indent, SerializableProperty property)
        {
            var propertyName = property.Name;

            if (property.Rule != nameof(PrimitiveTypeMigrationRule))
            {
                throw new ArgumentException($"Invalid rule applied to property {propertyName}.");
            }

            var ruleType = property.RuleArguments[0];

            if (!Enum.TryParse<SpecialType>(ruleType, out var specialType))
            {
                throw new ArgumentException($"Invalid rule state for property {propertyName} ({ruleType})");
            }

            if (specialType == SpecialType.System_DateTime && property.RuleArguments[1] == "DeltaTime")
            {
                source.AppendLine($"{indent}writer.WriteDeltaTime({propertyName});");
            }
            else
            {
                source.AppendLine($"{indent}writer.Write({propertyName});");
            }
        }
    }
}
