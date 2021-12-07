/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SymbolMetadata.Builtin.cs                                       *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Microsoft.CodeAnalysis;

namespace SerializationGenerator;

public static partial class SymbolMetadata
{
    public const string DICTIONARY_CLASS = "System.Collections.Generic.Dictionary`2";
    public const string LIST_CLASS = "System.Collections.Generic.List`1";
    public const string HASHSET_CLASS = "System.Collections.Generic.HashSet`1";
    public const string IPADDRESS_CLASS = "System.Net.IPAddress";
    public const string KEYVALUEPAIR_STRUCT = "System.Collections.Generic.KeyValuePair";
    public const string TIMESPAN_STRUCT = "System.TimeSpan";

    public static bool IsTimeSpan(this ISymbol symbol, Compilation compilation) =>
        symbol.Equals(
            compilation.GetTypeByMetadataName(TIMESPAN_STRUCT),
            SymbolEqualityComparer.Default
        );

    public static bool IsIpAddress(this ISymbol symbol, Compilation compilation) =>
        symbol.Equals(
            compilation.GetTypeByMetadataName(IPADDRESS_CLASS),
            SymbolEqualityComparer.Default
        );

    public static bool IsKeyValuePair(this ISymbol symbol, Compilation compilation) =>
        (symbol as INamedTypeSymbol)?.ConstructedFrom.Equals(
            compilation.GetTypeByMetadataName(KEYVALUEPAIR_STRUCT),
            SymbolEqualityComparer.Default
        ) == true;

    public static bool IsDictionary(this ISymbol symbol, Compilation compilation) =>
        (symbol as INamedTypeSymbol)?.ConstructedFrom.Equals(
            compilation.GetTypeByMetadataName(DICTIONARY_CLASS),
            SymbolEqualityComparer.Default
        ) == true;

    public static bool IsHashSet(this ISymbol symbol, Compilation compilation) =>
        (symbol as INamedTypeSymbol)?.ConstructedFrom.Equals(
            compilation.GetTypeByMetadataName(HASHSET_CLASS),
            SymbolEqualityComparer.Default
        ) == true;

    public static bool IsList(this ISymbol symbol, Compilation compilation) =>
        (symbol as INamedTypeSymbol)?.ConstructedFrom.Equals(
            compilation.GetTypeByMetadataName(LIST_CLASS),
            SymbolEqualityComparer.Default
        ) == true;

    public static bool IsPrimitiveFromTypeDisplayString(string type) =>
        type is "bool" or "sbyte" or "short" or "int" or "long" or "byte" or "ushort"
            or "uint" or "ulong" or "float" or "double" or "string" or "decimal";
}