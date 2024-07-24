/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Html.cs                                                         *
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
using System.Text;
using Server.Buffers;

namespace Server;

public static class Html
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Color(this string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawInterpolatedStringHandler Color(this ReadOnlySpan<char> text, int color) =>
        $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Color(this string text, int color, int size) => $"<BASEFONT COLOR=#{color:X6} SIZE={size}>{text}</BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Color(this string text, string color) => $"<BASEFONT COLOR={color}>{text}</BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Color(this string text, string color, int size) =>
        $"<BASEFONT COLOR={color} SIZE={size}>{text}</BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this string text) => $"<CENTER>{text}</CENTER>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawInterpolatedStringHandler Center(this ReadOnlySpan<char> text) => $"<CENTER>{text}</CENTER>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this string text, int color) =>
        $"<BASEFONT COLOR=#{color:X6}><CENTER>{text}</CENTER></BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawInterpolatedStringHandler Center(this ReadOnlySpan<char> text, int color) =>
        $"<BASEFONT COLOR=#{color:X6}><CENTER>{text}</CENTER></BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this string text, int color, int size) =>
        $"<BASEFONT COLOR=#{color:X6} SIZE={size}><CENTER>{text}</CENTER></BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this string text, string color) =>
        $"<BASEFONT COLOR={color}><CENTER>{text}</CENTER></BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this string text, string color, int size) =>
        $"<BASEFONT COLOR={color} SIZE={size}><CENTER>{text}</CENTER></BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(this string text) => $"<RIGHT>{text}</RIGHT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(this string text, int color) =>
        $"<BASEFONT COLOR=#{color:X6}><RIGHT>{text}</RIGHT></BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(this string text, int color, int size) =>
        $"<BASEFONT COLOR=#{color:X6} SIZE={size}><RIGHT>{text}</RIGHT></BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(this string text, string color) => $"<BASEFONT COLOR={color}><RIGHT>{text}</RIGHT></BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(this string text, string color, int size) =>
        $"<BASEFONT COLOR={color} SIZE={size}><RIGHT>{text}</RIGHT></BASEFONT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string EscapeHtml(this string input) =>
        new StringBuilder(input.Length).Append(input)
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;")
            .ToString();
}
