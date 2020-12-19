/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: Insensitive.cs                                                  *
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
using System.Runtime.CompilerServices;

namespace Server
{
    public static class Insensitive
    {
        public static IComparer<string> Comparer { get; } = StringComparer.OrdinalIgnoreCase;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Compare(string a, string b) => Comparer.Compare(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InsensitiveEquals(this ReadOnlySpan<char> a, string b) =>
            a.Equals(b, StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InsensitiveEquals(this string a, string b) =>
            a?.Equals(b, StringComparison.OrdinalIgnoreCase) ?? b == null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith(string a, string b) =>
            a != null && b != null && a.Length >= b.Length && Comparer.Compare(a.Substring(0, b.Length), b) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWith(string a, string b) =>
            a != null && b != null && a.Length >= b.Length && Comparer.Compare(a.Substring(a.Length - b.Length), b) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(string a, string b) =>
            a != null && b != null && a.Length >= b.Length && a.Contains(b, StringComparison.Ordinal);
    }
}
