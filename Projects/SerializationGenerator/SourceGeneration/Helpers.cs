/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Helpers.cs                                                      *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SerializationGenerator;

public static class Helpers
{
    public static bool ContainsInterface(this ITypeSymbol symbol, ISymbol interfaceSymbol) =>
        symbol.Interfaces.Any(i => i.ConstructedFrom.Equals(interfaceSymbol, SymbolEqualityComparer.Default)) ||
        symbol.AllInterfaces.Any(i => i.ConstructedFrom.Equals(interfaceSymbol, SymbolEqualityComparer.Default));

    public static ImmutableArray<IMethodSymbol> GetAllMethods(this ITypeSymbol symbol, string name)
    {
        var methods = symbol.GetMembers(name).OfType<IMethodSymbol>().ToImmutableArray();
        if (symbol.ContainingSymbol is not ITypeSymbol typeSymbol)
        {
            return methods;
        }

        var list = new List<IMethodSymbol>();
        list.AddRange(methods.ToList());
        list.AddRange(GetAllMethods(typeSymbol, name).ToList());

        return list.ToImmutableArray();
    }

    public static string ToFriendlyString(this Accessibility accessibility) => SyntaxFacts.GetText(accessibility);

    public static Accessibility GetAccessibility(string? value) =>
        value switch
        {
            "private"            => Accessibility.Private,
            "protected"          => Accessibility.Protected,
            "internal"           => Accessibility.Internal,
            "public"             => Accessibility.Public,
            "protected internal" => Accessibility.ProtectedOrInternal,
            "private protected"  => Accessibility.ProtectedAndInternal,
            _                    => Accessibility.NotApplicable
        };

    public static bool CanBeConstructedFrom(this ITypeSymbol? symbol, ISymbol classSymbol) =>
        symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.ConstructedFrom.Equals(
            classSymbol,
            SymbolEqualityComparer.Default
        ) || symbol != null && CanBeConstructedFrom(symbol.BaseType, classSymbol);
}