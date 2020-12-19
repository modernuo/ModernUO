/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: SensitiveStringHelpers.cs                                       *
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
    public static class OrdinalStringHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CompareOrdinal(this ReadOnlySpan<char> a, ReadOnlySpan<char> b) =>
            a.CompareTo(b, StringComparison.Ordinal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CompareOrdinal(this string a, string b) => StringComparer.Ordinal.Compare(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsOrdinal(this ReadOnlySpan<char> a, string b) =>
            a.Equals(b, StringComparison.Ordinal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsOrdinal(this string a, string b) =>
            a?.Equals(b, StringComparison.Ordinal) ?? b == null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWithOrdinal(this ReadOnlySpan<char> a, ReadOnlySpan<char> b) =>
            a.StartsWith(b, StringComparison.Ordinal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWithOrdinal(this string a, string b) =>
            a?.StartsWith(b, StringComparison.Ordinal) == true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWithOrdinal(this string a, string b) =>
            a?.EndsWith(b, StringComparison.Ordinal) == true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWithOrdinal(this ReadOnlySpan<char> a, ReadOnlySpan<char> b) =>
            a.EndsWith(b, StringComparison.Ordinal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsOrdinal(this ReadOnlySpan<char> a, string b) =>
            a.Contains(b, StringComparison.Ordinal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsOrdinal(this string a, string b) =>
            a?.Contains(b, StringComparison.Ordinal) == true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsOrdinal(this ReadOnlySpan<char> a, ReadOnlySpan<char> b) =>
            a.Contains(b, StringComparison.Ordinal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsOrdinal(this string a, char b) =>
            a?.Contains(b, StringComparison.Ordinal) == true;
    }
}
