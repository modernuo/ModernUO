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

namespace SerializationGenerator
{
    public static class Helpers
    {
        public static bool ContainsInterface(this ITypeSymbol symbol, ISymbol interfaceSymbol) =>
            symbol?.AllInterfaces.Any(i => i.Equals(interfaceSymbol, SymbolEqualityComparer.Default)) ?? false;

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
    }
}
