/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableEntityGeneration.MetadataTypes.cs                   *
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
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SerializationGenerator
{
    public static partial class SerializableEntityGeneration
    {
        public const string ICOLLECTION_INTERFACE = "System.Collections.Generic.ICollection`1";
        public const string ILIST_INTERFACE = "System.Collections.Generic.IList`1";
        public const string ISET_INTERFACE = "System.Collections.Generic.ISet`1";
        public const string IP_CLASS = "System.Net.IPAddress";

        public const string SERIALIZABLE_ATTRIBUTE = "Server.SerializableAttribute";
        public const string SERIALIZABLE_FIELD_ATTRIBUTE = "Server.SerializableFieldAttribute";
        public const string SERIALIZABLE_FIELD_ATTR_ATTRIBUTE = "Server.SerializableFieldAttrAttribute";
        public const string SERIALIZABLE_INTERFACE = "Server.ISerializable";
        public const string GENERIC_WRITER_INTERFACE = "Server.IGenericWriter";
        public const string GENERIC_READER_INTERFACE = "Server.IGenericReader";
        public const string DELTA_DATE_TIME_ATTRIBUTE = "Server.DeltaDateTimeAttribute";
        public const string POINT2D_STRUCT = "Server.Point2D";
        public const string POINT3D_STRUCT = "Server.Point3D";
        public const string RECTANGLE2D_STRUCT = "Server.Rectangle2D";
        public const string RECTANGLE3D_STRUCT = "Server.Rectangle3D";
        public const string RACE_CLASS = "Server.Race";
        public const string MAP_CLASS = "Server.Map";

        public static bool IsDeltaDateTime(this AttributeData attr, Compilation compilation) =>
            attr?.IsAttribute(compilation.GetTypeByMetadataName(DELTA_DATE_TIME_ATTRIBUTE)) == true;

        public static bool IsAttribute(this AttributeData attr, ISymbol symbol) =>
            attr?.AttributeClass?.Equals(symbol, SymbolEqualityComparer.Default) == true;

        public static bool IsEnum(this ITypeSymbol symbol) =>
            symbol.SpecialType == SpecialType.System_Enum || symbol.TypeKind == TypeKind.Enum;

        public static bool HasSerializableInterface(
            this ITypeSymbol symbol,
            Compilation compilation,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        ) =>
            symbol.ContainsInterface(compilation.GetTypeByMetadataName(SERIALIZABLE_INTERFACE)) ||
            serializableTypes.Contains(symbol);

        public static bool Contains(this ImmutableArray<INamedTypeSymbol> symbols, ITypeSymbol symbol) =>
            symbol is INamedTypeSymbol namedSymbol &&
            symbols.Contains(namedSymbol, SymbolEqualityComparer.Default);

        public static bool HasGenericReaderCtor(this INamedTypeSymbol symbol, Compilation compilation, out bool requiresParent)
        {
            var genericReaderInterface = compilation.GetTypeByMetadataName(GENERIC_READER_INTERFACE);
            var genericCtor = symbol.Constructors.FirstOrDefault(
                m => !m.IsStatic &&
                     m.MethodKind == MethodKind.Constructor &&
                     m.Parameters.Length <= 2 &&
                     m.Parameters[0].Equals(genericReaderInterface, SymbolEqualityComparer.Default)
            );

            requiresParent = genericCtor?.Parameters.Length == 2 && genericCtor.Parameters[1].Equals(symbol, SymbolEqualityComparer.Default);
            return genericCtor != null;
        }

        public static bool HasPublicSerializeMethod(
            this ITypeSymbol symbol,
            Compilation compilation,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {
            if (symbol.HasSerializableInterface(compilation, serializableTypes))
            {
                return true;
            }

            var genericWriterInterface = compilation.GetTypeByMetadataName(GENERIC_WRITER_INTERFACE);

            return symbol.GetAllMethods("Serialize")
                .Any(
                    m => !m.IsStatic &&
                         m.ReturnsVoid &&
                         m.Parameters.Length == 1 &&
                         m.Parameters[0].Equals(genericWriterInterface, SymbolEqualityComparer.Default) &&
                         m.DeclaredAccessibility == Accessibility.Public
                );
        }

        public static bool IsListOfSerializable(
            this INamedTypeSymbol symbol,
            Compilation compilation,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        ) => symbol.ContainsInterface(compilation.GetTypeByMetadataName("System.Collections.Generic.IList`1")) &&
             symbol.TypeArguments[0].HasSerializableInterface(compilation, serializableTypes);

        public static bool IsHashSetOfSerializable(
            this INamedTypeSymbol symbol,
            Compilation compilation,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        ) => symbol.ContainsInterface(compilation.GetTypeByMetadataName("System.Collections.Generic.ISet`1")) &&
             symbol.TypeArguments[0].HasSerializableInterface(compilation, serializableTypes);

        public static bool IsPoint2D(this ISymbol symbol, Compilation compilation) =>
            symbol.Equals(
                compilation.GetTypeByMetadataName(POINT2D_STRUCT),
                SymbolEqualityComparer.Default
            );

        public static bool IsPoint3D(this ISymbol symbol, Compilation compilation) =>
            symbol.Equals(
                compilation.GetTypeByMetadataName(POINT3D_STRUCT),
                SymbolEqualityComparer.Default
            );

        public static bool IsRectangle2D(this ISymbol symbol, Compilation compilation) =>
            symbol.Equals(
                compilation.GetTypeByMetadataName(RECTANGLE2D_STRUCT),
                SymbolEqualityComparer.Default
            );

        public static bool IsRectangle3D(this ISymbol symbol, Compilation compilation) =>
            symbol.Equals(
                compilation.GetTypeByMetadataName(RECTANGLE3D_STRUCT),
                SymbolEqualityComparer.Default
            );

        public static bool IsIpAddress(this ISymbol symbol, Compilation compilation) =>
            symbol.Equals(
                compilation.GetTypeByMetadataName(IP_CLASS),
                SymbolEqualityComparer.Default
            );

        public static bool IsRace(this ISymbol symbol, Compilation compilation) =>
            symbol.Equals(
                compilation.GetTypeByMetadataName(RACE_CLASS),
                SymbolEqualityComparer.Default
            );

        public static bool IsMap(this ISymbol symbol, Compilation compilation) =>
            symbol.Equals(
                compilation.GetTypeByMetadataName(MAP_CLASS),
                SymbolEqualityComparer.Default
            );
    }
}
