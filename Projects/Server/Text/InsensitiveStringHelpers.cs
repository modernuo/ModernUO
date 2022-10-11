/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: InsensitiveStringHelpers.cs                                     *
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

namespace Server;

public static class InsensitiveStringHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int InsensitiveCompare(this ReadOnlySpan<char> a, ReadOnlySpan<char> b) =>
        a.CompareTo(b, StringComparison.OrdinalIgnoreCase);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int InsensitiveCompare(this string a, string b) => StringComparer.Ordinal.Compare(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InsensitiveEquals(this ReadOnlySpan<char> a, string b) =>
        a.Equals(b, StringComparison.OrdinalIgnoreCase);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InsensitiveEquals(this string a, string b) =>
        a?.Equals(b, StringComparison.OrdinalIgnoreCase) ?? b == null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InsensitiveStartsWith(this ReadOnlySpan<char> a, ReadOnlySpan<char> b) =>
        a.StartsWith(b, StringComparison.OrdinalIgnoreCase);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InsensitiveStartsWith(this string a, string b) =>
        a?.StartsWith(b, StringComparison.OrdinalIgnoreCase) == true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InsensitiveEndsWith(this string a, string b) =>
        a?.EndsWith(b, StringComparison.OrdinalIgnoreCase) == true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InsensitiveEndsWith(this ReadOnlySpan<char> a, ReadOnlySpan<char> b) =>
        a.EndsWith(b, StringComparison.OrdinalIgnoreCase);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InsensitiveContains(this ReadOnlySpan<char> a, string b) =>
        a.Contains(b, StringComparison.OrdinalIgnoreCase);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InsensitiveContains(this string a, string b) =>
        a?.Contains(b, StringComparison.OrdinalIgnoreCase) == true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InsensitiveContains(this ReadOnlySpan<char> a, ReadOnlySpan<char> b) =>
        a.Contains(b, StringComparison.OrdinalIgnoreCase);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InsensitiveContains(this string a, char b) =>
        a?.Contains(b, StringComparison.OrdinalIgnoreCase) == true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int InsensitiveIndexOf(this string a, char b) =>
        a?.IndexOf(b, StringComparison.OrdinalIgnoreCase) ?? -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int InsensitiveIndexOf(this string a, string b) =>
        a?.IndexOf(b, StringComparison.OrdinalIgnoreCase) ?? -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int InsensitiveIndexOf(this string a, string b, int startIndex) =>
        a?.IndexOf(b, startIndex, StringComparison.OrdinalIgnoreCase) ?? -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int InsensitiveIndexOf(this ReadOnlySpan<char> a, ReadOnlySpan<char> b) =>
        a.IndexOf(b, StringComparison.OrdinalIgnoreCase);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string InsensitiveReplace(this string a, string o, string n) =>
        a?.Replace(o, n, StringComparison.OrdinalIgnoreCase);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InsensitiveRemove(
        this ReadOnlySpan<char> a,
        ReadOnlySpan<char> b,
        Span<char> buffer,
        out int size
    ) => a.Remove(b, StringComparison.OrdinalIgnoreCase, buffer, out size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string InsensitiveRemove(this ReadOnlySpan<char> a, ReadOnlySpan<char> b) =>
        a.Remove(b, StringComparison.OrdinalIgnoreCase);
}
