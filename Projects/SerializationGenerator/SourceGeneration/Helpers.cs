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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace SerializationGenerator
{
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

        public static INamespaceSymbol GetNamespace(this ISymbol symbol)
        {
            while (true)
            {
                if (symbol is INamespaceSymbol namespaceSymbol && namespaceSymbol.IsGlobalNamespace)
                {
                    return namespaceSymbol;
                }

                if (symbol.ContainingSymbol != null)
                {
                    symbol = symbol.ContainingSymbol;
                }
            }
        }

        public static void GetNamespaces(this string fullyQualifiedName, Compilation compilation, HashSet<string> namespaces)
        {
            var parts = fullyQualifiedName.Split('.');
            var namespacePart = "";

            for (int i = 0; i < parts.Length; i++)
            {
                var newNamespace = $"{namespacePart}{parts[0]}";
                if (compilation.GetTypeByMetadataName(newNamespace)?.IsNamespace != true)
                {
                    namespaces.Add(namespacePart);
                    break;
                }

                namespacePart = newNamespace;
            }

            // Get namespaces for generics by recursion
        }
    }
}
