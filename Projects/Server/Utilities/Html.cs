/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
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
using System.Buffers;
using System.Runtime.CompilerServices;
using Server.Buffers;
using Server.Text;

namespace Server;

public enum TextAlignment : byte
{
    Left = 0,
    Center = 1,
    Right = 2
}

public static class Html
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this string input) => Center(input.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this ReadOnlySpan<char> input) => $"<CENTER>{input}</CENTER>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(ref RawInterpolatedStringHandler input)
    {
        var str = input.Text.Center();
        input.Clear();
        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this string input, int color) => Center(input.AsSpan(), color);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this string input, ReadOnlySpan<char> color) => Center(input.AsSpan(), color);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this ReadOnlySpan<char> input, int color) =>
        $"<CENTER><BASEFONT COLOR=#{color:X6}>{input}</BASEFONT></CENTER>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this ReadOnlySpan<char> input, ReadOnlySpan<char> color) =>
        $"<CENTER><BASEFONT COLOR={color}>{input}</BASEFONT></CENTER>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(ref RawInterpolatedStringHandler input, int color)
    {
        var str = input.Text.Center(color);
        input.Clear();
        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(ref RawInterpolatedStringHandler input, ReadOnlySpan<char> color)
    {
        var str = input.Text.Center(color);
        input.Clear();
        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Color(this string input, int color) => Color(input.AsSpan(), color);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Color(this string input, ReadOnlySpan<char> color) => Color(input.AsSpan(), color);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Color(this ReadOnlySpan<char> input, int color) => $"<BASEFONT COLOR=#{color:X6}>{input}</CENTER>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Color(this ReadOnlySpan<char> input, ReadOnlySpan<char> color) =>
        $"<BASEFONT COLOR={color}>{input}</CENTER>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Color(ref RawInterpolatedStringHandler input, int color)
    {
        var str = input.Text.Color(color);
        input.Clear();
        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Color(ref RawInterpolatedStringHandler input, ReadOnlySpan<char> color)
    {
        var str = input.Text.Color(color);
        input.Clear();
        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(this string input) => Right(input.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(this ReadOnlySpan<char> input) => $"<RIGHT>{input}</RIGHT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(ref RawInterpolatedStringHandler input)
    {
        var str = input.Text.Right();
        input.Clear();
        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(this string input, int color) => Right(input.AsSpan(), color);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(this string input, ReadOnlySpan<char> color) => Right(input.AsSpan(), color);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(this ReadOnlySpan<char> input, int color) =>
        $"<RIGHT><BASEFONT COLOR=#{color:X6}>{input}</BASEFONT></RIGHT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(this ReadOnlySpan<char> input, ReadOnlySpan<char> color) =>
        $"<RIGHT><BASEFONT COLOR={color}>{input}</BASEFONT></RIGHT>";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(ref RawInterpolatedStringHandler input, int color)
    {
        var str = input.Text.Right(color);
        input.Clear();
        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(ref RawInterpolatedStringHandler input, ReadOnlySpan<char> color)
    {
        var str = input.Text.Right(color);
        input.Clear();
        return str;
    }

    private static readonly SearchValues<char> _htmlSearchValues = SearchValues.Create('<', '>', '&', '"', '\'');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string EscapeHtml(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input ?? "";
        }

        return EscapeHtml(input.AsSpan());
    }

    public static string EscapeHtml(this ReadOnlySpan<char> input)
    {
        if (input.IsEmpty)
        {
            return string.Empty;
        }

        int indexOfAny = input.IndexOfAny(_htmlSearchValues);
        if (indexOfAny < 0)
        {
            return input.ToString();
        }

        using var builder = ValueStringBuilder.Create(input.Length * 2);
        int lastIndex = 0;

        while (indexOfAny >= 0)
        {
            if (indexOfAny > lastIndex)
            {
                builder.Append(input[lastIndex..indexOfAny]);
            }

            char c = input[indexOfAny];
            var replacement = c switch
            {
                '&'  => "&amp;",
                '<'  => "&lt;",
                '>'  => "&gt;",
                '"'  => "&quot;",
                '\'' => "&#39;"
            };
            builder.Append(replacement);

            lastIndex = indexOfAny + 1;
            indexOfAny = input[lastIndex..].IndexOfAny(_htmlSearchValues);
            if (indexOfAny < 0)
            {
                break;
            }

            indexOfAny += lastIndex;
        }

        if (lastIndex < input.Length)
        {
            builder.Append(input[lastIndex..]);
        }

        var result = builder.ToString();
        builder.Dispose();
        return result;
    }

    public static string Build(
        ReadOnlySpan<char> text, ReadOnlySpan<char> color = default, int size = -1, byte fontStyle = 0,
        TextAlignment align = TextAlignment.Left
    )
    {
        var arr = STArrayPool<char>.Shared.Rent(BuildCharCount(text, color));
        var bytesWritten = Build(text, arr.AsSpan(), color, size, fontStyle, align);

        var result = arr.AsSpan(0, bytesWritten).ToString();
        STArrayPool<char>.Shared.Return(arr);

        return result;
    }

    public static int BuildCharCount(ReadOnlySpan<char> text, ReadOnlySpan<char> color) => 61 + text.Length + color.Length;

    public static int Build(
        ReadOnlySpan<char> text, Span<char> dest, ReadOnlySpan<char> color = default, int size = -1, byte fontStyle = 0,
        TextAlignment align = TextAlignment.Left
    )
    {
        using var builder = new ValueStringBuilder(dest);
        if (align == TextAlignment.Right)
        {
            builder.Append("<RIGHT>");
        }
        else if (align == TextAlignment.Center)
        {
            builder.Append("<CENTER>");
        }

        if (color.Length > 0 || size > -1 || fontStyle > 0)
        {
            builder.Append("<BASEFONT");
            if (color.Length > 0)
            {
                builder.Append($" COLOR={color}");
            }

            if (size > -1)
            {
                builder.Append($" SIZE={size}");
            }

            if (fontStyle > 0)
            {
                builder.Append($" STYLE={fontStyle}");
            }
            builder.Append($">{text}</BASEFONT>");
        }
        else
        {
            builder.Append(text);
        }

        if (align == TextAlignment.Right)
        {
            builder.Append("</RIGHT>");
        }
        else if (align == TextAlignment.Center)
        {
            builder.Append("</CENTER>");
        }

        return builder.Length;
    }
}
