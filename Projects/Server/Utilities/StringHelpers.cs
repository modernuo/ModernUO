/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
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
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Server
{
    public static class StringHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string DefaultIfNullOrEmpty(this string value, string def) => value?.Trim().Length > 0 ? value : def;

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

                sliced.Slice(0, indexOf).CopyTo(buffer.Slice(size));
                size += indexOf;

                if (indexOf == sliced.Length)
                {
                    break;
                }

                sliced = sliced.Slice(indexOf + 1);
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
                chrs = ArrayPool<char>.Shared.Rent(a.Length);
                span = chrs.AsSpan();
            }
            else
            {
                chrs = null;
            }

            a.Remove(b, comparison, span, out var size);

            var str = span.Slice(0, size).ToString();

            if (chrs != null)
            {
                ArrayPool<char>.Shared.Return(chrs);
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
                chrs = ArrayPool<char>.Shared.Rent(value.Length);
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
                    sliced = sliced.Slice(4);
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

                sliced = sliced.Slice(indexOf + 1);
                index += indexOf + 1;
            }

            var str = span.ToString();

            if (chrs != null)
            {
                ArrayPool<char>.Shared.Return(chrs);
            }

            return str;
        }
    }
}
