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
                ruleArguments = Array.Empty<string>();
                return true;
            }

            if (
                symbol is not ITypeSymbol {
                    SpecialType: not (not
                    SpecialType.System_Boolean and not
                    SpecialType.System_SByte and not
                    SpecialType.System_Int16 and not
                    SpecialType.System_Int32 and not
                    SpecialType.System_Int64 and not
                    SpecialType.System_Byte and not
                    SpecialType.System_UInt16 and not
                    SpecialType.System_UInt32 and not
                    SpecialType.System_UInt64 and not
                    SpecialType.System_Single and not
                    SpecialType.System_Double and not
                    SpecialType.System_String and not
                    SpecialType.System_Decimal and not
                    SpecialType.System_DateTime)
                } typeSymbol
            )
            {
                ruleArguments = null;
                return false;
            }

            if (typeSymbol.SpecialType == SpecialType.System_DateTime && attributes.Any(a => a.IsDeltaDateTime(compilation)))
            {
                ruleArguments = new[] { "DeltaTime" };
            }
            else if (typeSymbol.SpecialType == SpecialType.System_String && attributes.Any(a => a.IsInternString(compilation)))
            {
                ruleArguments = new[] { "InternString" };
            }
            else
            {
                ruleArguments = Array.Empty<string>();
            }

            return true;
        }

        public void GenerateDeserializationMethod(StringBuilder source, string indent, SerializableProperty property)
        {
            const string expectedRule = nameof(PrimitiveTypeMigrationRule);
            var ruleName = property.Rule;
            if (expectedRule != ruleName)
            {
                throw new ArgumentException($"Invalid rule applied to property {ruleName}. Expecting {expectedRule}, but received {ruleName}.");
            }

            var propertyName = property.Name;
            string readMethod;

            const string ipAddress = SerializableEntityGeneration.IPADDRESS_CLASS;

            readMethod = property.Type switch
            {
                "bool"    => "ReadBool",
                "sbyte"   => "ReadSByte",
                "short"   => "ReadShort",
                "int"     => "ReadInt",
                "long"    => "ReadLong",
                "byte"    => "ReadByte",
                "ushort"  => "ReadUShort",
                "uint"    => "ReadUInt",
                "ulong"   => "ReadULong",
                "float"    => "ReadFloat",
                "double"  => "ReadDouble",
                "string"  => "ReadString",
                "decimal" => "ReadDecimal",
                ipAddress => "ReadIPAddress",
                "System.DateTime" => property.RuleArguments.Length >= 1 &&
                                     property.RuleArguments[0] == "DeltaTime" ?
                    "ReadDeltaTime" :
                    "ReadDateTime"
            };

            var readArgument = readMethod == "ReadString" &&
                               property.RuleArguments.Length >= 1 &&
                               property.RuleArguments[0] == "InternString" ? "true" : "";

            source.AppendLine($"{indent}{propertyName} = reader.{readMethod}({readArgument});");
        }

        public void GenerateSerializationMethod(StringBuilder source, string indent, SerializableProperty property)
        {
            const string expectedRule = nameof(PrimitiveTypeMigrationRule);
            var ruleName = property.Rule;
            if (expectedRule != ruleName)
            {
                throw new ArgumentException($"Invalid rule applied to property {ruleName}. Expecting {expectedRule}, but received {ruleName}.");
            }

            var propertyName = property.Name;

            if (property.Type == "System.DateTime" && property.RuleArguments.Length >= 1 && property.RuleArguments[0] == "DeltaTime")
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
