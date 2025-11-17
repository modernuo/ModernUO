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

public static class Html
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawInterpolatedStringHandler Color(
        scoped ref RawInterpolatedStringHandler textHandler,
        ReadOnlySpan<char> color,
        int size = -1, byte fontStyle = 0
    )
    {
        var handler = textHandler.Text.Color(color, size, fontStyle);
        textHandler.Clear();
        return handler;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawInterpolatedStringHandler Color(
        this ReadOnlySpan<char> text,
        ReadOnlySpan<char> color,
        int size = -1,
        byte fontStyle = 0
    )
    {
        if (color != Span<char>.Empty)
        {
            if (size > -1)
            {
                if (fontStyle > 0)
                {
                    return $"<BASEFONT COLOR={color} SIZE={size} STYLE={fontStyle}>{text}</BASEFONT>";
                }

                return $"<BASEFONT COLOR={color} SIZE={size}>{text}</BASEFONT>";
            }

            if (fontStyle > 0)
            {
                return $"<BASEFONT COLOR={color} STYLE={fontStyle}>{text}</BASEFONT>";
            }

            return $"<BASEFONT COLOR={color}>{text}</BASEFONT>";
        }

        if (size > -1)
        {
            if (fontStyle > 0)
            {
                return $"<BASEFONT SIZE={size} STYLE={fontStyle}>{text}</BASEFONT>";
            }

            return $"<BASEFONT SIZE={size}>{text}</BASEFONT>";
        }

        if (fontStyle > 0)
        {
            return $"<BASEFONT STYLE={fontStyle}>{text}</BASEFONT>";
        }

        return $"{text}";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawInterpolatedStringHandler Color(
        this ReadOnlySpan<char> text,
        int color,
        int size = -1,
        byte fontStyle = 0
    )
    {
        if (color > -1)
        {
            return text.Color($"#{color:X6}", size, fontStyle);
        }

        return text.Color((ReadOnlySpan<char>)default, size, fontStyle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawInterpolatedStringHandler Color(
        scoped ref RawInterpolatedStringHandler textHandler,
        int color,
        int size = -1,
        byte fontStyle = 0
    )
    {
        var handler = textHandler.Text.Color(color, size, fontStyle);
        textHandler.Clear();
        return handler;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Color(
        this string text,
        ReadOnlySpan<char> color,
        int size = -1,
        byte fontStyle = 0
    )
    {
        var textHandler = ((ReadOnlySpan<char>)text).Color(color, size, fontStyle);
        var str = textHandler.Text.ToString();
        textHandler.Clear();
        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Color(
        this string text,
        int color,
        int size = -1,
        byte fontStyle = 0
    )
    {
        var textHandler = ((ReadOnlySpan<char>)text).Color(color, size, fontStyle);
        var str = textHandler.Text.ToString();
        textHandler.Clear();
        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(
        this string text, int color, int size = -1, byte fontStyle = 0
    )
    {
        var handler = Center((ReadOnlySpan<char>)text, color, size, fontStyle);
        var str = handler.Text.ToString();
        handler.Clear();

        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(
        this string text, ReadOnlySpan<char> color, int size = -1, byte fontStyle = 0
    )
    {
        var handler = Center((ReadOnlySpan<char>)text, color, size, fontStyle);
        var str = handler.Text.ToString();
        handler.Clear();

        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Center(this string text) => text.Center(-1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawInterpolatedStringHandler Center(this ReadOnlySpan<char> text) => Center(text, -1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawInterpolatedStringHandler Center(
        this ReadOnlySpan<char> text, int color, int size = -1, byte fontStyle = 0
    ) => Color($"<CENTER>{text}</CENTER>", color, size, fontStyle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawInterpolatedStringHandler Center(
        this ReadOnlySpan<char> text, ReadOnlySpan<char> color, int size = -1, byte fontStyle = 0
    ) => Color($"<CENTER>{text}</CENTER>", color, size, fontStyle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawInterpolatedStringHandler Center(
        scoped ref RawInterpolatedStringHandler textHandler, int color = -1, int size = -1, byte fontStyle = 0
    )
    {
        var handler = textHandler.Text.Center(color, size, fontStyle);
        textHandler.Clear();
        return handler;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(
        this string text, int color, int size = -1, byte fontStyle = 0
    )
    {
        var handler = Right((ReadOnlySpan<char>)text, color, size, fontStyle);
        var str = handler.Text.ToString();
        handler.Clear();

        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(
        this string text, ReadOnlySpan<char> color, int size = -1, byte fontStyle = 0
    )
    {
        var handler = Right((ReadOnlySpan<char>)text, color, size, fontStyle);
        var str = handler.Text.ToString();
        handler.Clear();

        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Right(this string text) => text.Right(-1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawInterpolatedStringHandler Right(
        scoped ref RawInterpolatedStringHandler textHandler, int color = -1, int size = -1, byte fontStyle = 0
    )
    {
        var handler = textHandler.Text.Right(color, size, fontStyle);
        textHandler.Clear();
        return handler;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawInterpolatedStringHandler Right(
        this ReadOnlySpan<char> text, int color, int size = -1, byte fontStyle = 0
    ) => Color($"<RIGHT>{text}</RIGHT>", color, size, fontStyle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawInterpolatedStringHandler Right(
        this ReadOnlySpan<char> text, ReadOnlySpan<char> color, int size = -1, byte fontStyle = 0
    ) => Color($"<RIGHT>{text}</RIGHT>", color, size, fontStyle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawInterpolatedStringHandler Right(this ReadOnlySpan<char> text) => text.Right(-1);

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
}
