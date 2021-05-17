/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableEntityGeneration.DeserializeMethod.cs               *
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
    public static partial class SerializableEntityGeneration
    {
        public static void GenerateDeserializeMethod(
            this StringBuilder source,
            Compilation compilation,
            bool isOverride,
            int version,
            ImmutableArray<IFieldSymbol> fields,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {
            var genericReaderInterface = compilation.GetTypeByMetadataName(GENERIC_READER_INTERFACE);

            source.GenerateMethodStart(
                "Deserialize",
                AccessModifier.Public,
                isOverride,
                "void",
                ImmutableArray.Create<(ITypeSymbol, string)>((genericReaderInterface, "reader"))
            );

            const string indent = "            ";

            // Version
            source.AppendLine($"{indent}var version = reader.ReadEncodedInt();");
            for (var i = 0; i < version; i++)
            {
                source.AppendLine($"{indent}if (version == {i})");
                source.AppendLine($@"{indent}{{");
                source.AppendLine($"{indent}    MigrateFrom(new Version{i}Content(reader));");
                source.AppendLine($"{indent}    MarkDirty();");
                source.AppendLine($"{indent}    return;");
                source.AppendLine($@"{indent}}}");
            }

            var serializableProperties = fields.Select(
                f => new SerializableProperty
                {
                    Name = f.ToString(),
                    Type = (INamedTypeSymbol)f.Type
                }
            ).ToImmutableArray();

            foreach (var field in fields)
            {
                var serializableProperty = new SerializableProperty
                {
                    Name = field.GetPropertyName(),
                    Type = (INamedTypeSymbol)field.Type
                };

                var attributes = field.GetAttributes();

                source.DeserializeField($"{indent}    ", serializableProperty, compilation, attributes, serializableTypes);
            }

            source.GenerateMethodEnd();
        }

        public static void DeserializeField(
            this StringBuilder source,
            string indent,
            SerializableProperty property,
            Compilation compilation,
            ImmutableArray<AttributeData> attributes,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {

        }

        private static string GetDeserializeReaderMethod(
            this ITypeSymbol symbol,
            Compilation compilation,
            ImmutableArray<AttributeData> attributes,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {
            switch (symbol.SpecialType)
            {
                case SpecialType.System_Boolean:  return "ReadBool";
                case SpecialType.System_SByte:    return "ReadSByte";
                case SpecialType.System_Int16:    return "ReadShort";
                case SpecialType.System_Int32:    return "ReadInt";
                case SpecialType.System_Int64:    return "ReadLong";
                case SpecialType.System_Byte:     return "ReadByte";
                case SpecialType.System_UInt16:   return "ReadUShort";
                case SpecialType.System_UInt32:   return "ReadUInt";
                case SpecialType.System_UInt64:   return "ReadULong";
                case SpecialType.System_Single:   return "ReadFloat";
                case SpecialType.System_Double:   return "ReadDouble";
                case SpecialType.System_String:   return "ReadString";
                case SpecialType.System_Decimal:  return "ReadDecimal";
                case SpecialType.System_DateTime:
                    {
                        return attributes.Any(a => a.IsDeltaDateTime(compilation)) ?
                            "ReadDeltaTime" :
                            "ReadDateTime";
                    }
            }

            if (symbol.IsPoint2D(compilation))
            {
                return "ReadPoint2D";
            }

            if (symbol.IsPoint3D(compilation))
            {
                return "ReadPoint3D";
            }

            if (symbol.IsRectangle2D(compilation))
            {
                return "ReadRect2D";
            }

            if (symbol.IsRectangle3D(compilation))
            {
                return "ReadPoint3D";
            }

            if (symbol.IsIpAddress(compilation))
            {
                return "ReadIPAddress";
            }

            if (symbol.IsRace(compilation))
            {
                return "ReadRace";
            }

            if (symbol.IsMap(compilation))
            {
                return "ReadMap";
            }

            if (symbol.HasSerializableInterface(compilation, serializableTypes))
            {
                return $"ReadEntity<{symbol.Name}>";
            }

            if (symbol.IsListOfSerializable(compilation, serializableTypes))
            {
                return $"ReadEntityList<{symbol.Name}>";
            }

            if (symbol.IsHashSetOfSerializable(compilation, serializableTypes))
            {
                return $"ReadEntitySet<{symbol.Name}>";
            }

            if (symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.HasGenericReaderCtor(compilation, out var requiresParent))
            {
                return $"new {namedTypeSymbol}(reader{(requiresParent ? ", this" : "")})";
            }

            throw new Exception($"No serialization Read method for type {symbol.Name}");
        }
    }
}
