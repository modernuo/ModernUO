/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TypedSymbolEqualityComparer.cs                                  *
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
using Microsoft.CodeAnalysis;

namespace SerializationGenerator
{
    public class TypedSymbolEqualityComparer : IEqualityComparer<INamespaceSymbol>
    {
        public bool Equals(INamespaceSymbol x, INamespaceSymbol y) => SymbolEqualityComparer.IncludeNullability.Equals(x, y);
        public int GetHashCode(INamespaceSymbol obj) => obj.ToDisplayString().GetHashCode();
    }
}
