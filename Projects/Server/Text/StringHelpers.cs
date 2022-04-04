/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: StringHelpers.cs                                                *
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
using System.Runtime.InteropServices;
using Server.Buffers;

namespace Server;

public static class StringHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DefaultIfNullOrEmpty(this string value, string def) =>
        string.IsNullOrWhiteSpace(value) ? def : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remove(
        this ReadOnlySpan<char> a,
        ReadOnlySpan<char> b,
        StringComparison comparison,
        Span<char> buffer,
        out int size
    )
    {
        size = 0;
        if (a == null || a.Length == 0)
        {
            return;
        }

        var sliced = a;

        while (true)
        {
            var indexOf = sliced.IndexOf(b, comparison);
            if (indexOf == -1)
            {
                indexOf = sliced.Length;
            }

            if (size + indexOf > buffer.Length)
            {
                throw new OutOfMemoryException(nameof(buffer));
            }

            sliced[..indexOf].CopyTo(buffer[size..]);
            size += indexOf;

            if (indexOf == sliced.Length)
            {
                break;
            }

            sliced = sliced[(indexOf + 1)..];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Remove(this ReadOnlySpan<char> a, ReadOnlySpan<char> b, StringComparison comparison)
    {
        if (a == null)
        {
            return null;
        }

        if (a.Length == 0)
        {
            return "";
        }

        Span<char> span = a.Length < 1024 ? stackalloc char[a.Length] : null;
        char[] chrs;
        if (span == null)
        {
            chrs = STArrayPool<char>.Shared.Rent(a.Length);
            span = chrs.AsSpan();
        }
        else
        {
            chrs = null;
        }

        a.Remove(b, comparison, span, out var size);

        var str = span[..size].ToString();

        if (chrs != null)
        {
            STArrayPool<char>.Shared.Return(chrs);
        }

        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Capitalize(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        Span<char> span = value.Length < 1024 ? stackalloc char[value.Length] : null;
        char[] chrs;
        if (span == null)
        {
            chrs = STArrayPool<char>.Shared.Rent(value.Length);
            span = chrs.AsSpan();
        }
        else
        {
            chrs = null;
        }

        var sliced = value.AsSpan();
        // Copy over the previous span
        sliced.CopyTo(span);

        var index = 0;

        while (true)
        {
            // Special case for titles - words that don't get capitalized
            if (sliced.InsensitiveStartsWith("the "))
            {
                sliced = sliced[4..];
                index += 4;
                continue;
            }

            var indexOf = sliced.IndexOf(' ');
            span[index] = char.ToUpperInvariant(sliced[0]);

            if (indexOf == -1)
            {
                break;
            }

            if (indexOf == sliced.Length - 1)
            {
                break;
            }

            sliced = sliced[(indexOf + 1)..];
            index += indexOf + 1;
        }

        var str = span.ToString();

        if (chrs != null)
        {
            STArrayPool<char>.Shared.Return(chrs);
        }

        return str;
    }

    public static string TrimMultiline(this string str, string lineSeparator = "\n")
    {
        var parts = str.Split(lineSeparator);
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = parts[i].Trim();
        }

        return string.Join(lineSeparator, parts);
    }

    public static string IndentMultiline(this string str, string indent = "\t", string lineSeparator = "\n")
    {
        var parts = str.Split(lineSeparator);
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = $"{indent}{parts[i]}";
        }

        return string.Join(lineSeparator, parts);
    }

    public static List<string> Wrap(this string value, int perLine, int maxLines)
    {
        if ((value = value?.Trim() ?? "").Length <= 0)
        {
            return null;
        }

        var span = value.AsSpan();
        var list = new List<string>(maxLines);
        var lineLength = 0;

        while (span.Length > 0)
        {
            var spaceIndex = span[lineLength..].IndexOf(' ');
            if (spaceIndex == -1)
            {
                spaceIndex = span.Length - lineLength; // End of the string
            }

            var newLineLength = lineLength + spaceIndex;

            // If the previous line is exactly perLine or not too long and we are at the end
            if (newLineLength == perLine || newLineLength < perLine && newLineLength == span.Length)
            {
                list.Add(span[..newLineLength].ToString());
                if (list.Count == maxLines || newLineLength == span.Length)
                {
                    break;
                }

                span = span[(newLineLength + 1)..];
                lineLength = 0;
            }
            // We haven't hit perLine and are not sure if we can continue adding more words without going over
            else if (newLineLength < perLine)
            {
                lineLength = newLineLength + 1;
            }
            // We already tried making the line longer, and it was too long, so fall back to the old line
            else if (lineLength > 0 && lineLength <= perLine)
            {
                list.Add(span[..(lineLength - 1)].ToString());
                if (list.Count == maxLines)
                {
                    break;
                }

                span = span[lineLength..];
                lineLength = 0;
            }
            // We have a really long single word with no spaces and have to forcibly break it up.
            else
            {
                lineLength = newLineLength;
                var index = 0;

                while (index < lineLength)
                {
                    lineLength -= perLine;

                    var length = perLine - (span[index] == ' ' ? 1 : 0);
                    list.Add(span.Slice(index, length).ToString());
                    if (list.Count == maxLines)
                    {
                        break;
                    }

                    index += perLine;
                }

                span = span[(newLineLength - lineLength)..];
            }
        }

        return list;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfTerminator(this Span<byte> buffer, int sizeT) =>
        sizeT switch
        {
            2 => MemoryMarshal.Cast<byte, char>(buffer).IndexOf((char)0) * 2,
            4 => MemoryMarshal.Cast<byte, uint>(buffer).IndexOf((uint)0) * 4,
            _ => buffer.IndexOf((byte)0)
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfTerminator(this ReadOnlySpan<byte> buffer, int sizeT) =>
        sizeT switch
        {
            2 => MemoryMarshal.Cast<byte, char>(buffer).IndexOf((char)0) * 2,
            4 => MemoryMarshal.Cast<byte, uint>(buffer).IndexOf((uint)0) * 4,
            _ => buffer.IndexOf((byte)0)
        };
}
