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
using System.Runtime.CompilerServices;

namespace Server
{
    public static class Insensitive
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int InsensitiveCompare(this ReadOnlySpan<char> a, string b) =>
            a.CompareTo(b, StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int InsensitiveCompare(this string a, string b) => StringComparer.OrdinalIgnoreCase.Compare(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InsensitiveEquals(this ReadOnlySpan<char> a, string b) =>
            a.Equals(b, StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InsensitiveEquals(this string a, string b) =>
            a?.Equals(b, StringComparison.OrdinalIgnoreCase) ?? b == null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InsensitiveStartsWith(this ReadOnlySpan<char> a, string b) =>
            a.StartsWith(b, StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InsensitiveStartsWith(this string a, string b) =>
            a?.StartsWith(b, StringComparison.OrdinalIgnoreCase) == true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InsensitiveEndsWith(this ReadOnlySpan<char> a, string b) =>
            a.EndsWith(b, StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InsensitiveEndsWith(this string a, string b) =>
            a?.EndsWith(b, StringComparison.OrdinalIgnoreCase) == true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InsensitiveContains(this ReadOnlySpan<char> a, string b) =>
            a.Contains(b, StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InsensitiveContains(this string a, string b) =>
            a?.Contains(b, StringComparison.OrdinalIgnoreCase) == true;
    }
}
