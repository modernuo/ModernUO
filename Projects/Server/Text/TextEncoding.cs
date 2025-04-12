/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TextEncoding.cs                                                 *
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

namespace Server.Text;

public static class TextEncoding
{
    private static Encoding m_UTF8, m_Unicode, m_UnicodeLE;

    public static Encoding UTF8 => m_UTF8 ??= new UTF8Encoding(false, false);
    public static Encoding Unicode => m_Unicode ??= new UnicodeEncoding(true, false, false);
    public static Encoding UnicodeLE => m_UnicodeLE ??= new UnicodeEncoding(false, false, false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytesAscii(this string str) => GetBytes(str, Encoding.ASCII);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytesBigUni(this string str) => GetBytes(str, Unicode);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytesLittleUni(this string str) => GetBytes(str, UnicodeLE);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytesUtf8(this string str) => GetBytes(str, UTF8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytesAscii(this ReadOnlySpan<char> str) => GetBytes(str, Encoding.ASCII);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytesBigUni(this ReadOnlySpan<char> str) => GetBytes(str, Unicode);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytesLittleUni(this ReadOnlySpan<char> str) => GetBytes(str, UnicodeLE);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytesUtf8(this ReadOnlySpan<char> str) => GetBytes(str, UTF8);

    // Unlike the one built into the encoder, this avoids local init
    public static byte[] GetBytes(ReadOnlySpan<char> str, Encoding encoding)
    {
        if (str.Length == 0)
        {
            return Array.Empty<byte>();
        }

        var bytes = GC.AllocateUninitializedArray<byte>(encoding.GetByteCount(str));
        encoding.GetBytes(str, bytes);

        return bytes;
    }

    // Unlike the one built into the encoder, this avoids local init
    public static byte[] GetBytes(string str, Encoding encoding)
    {
        if (str.Length == 0)
        {
            return Array.Empty<byte>();
        }

        var bytes = GC.AllocateUninitializedArray<byte>(encoding.GetByteCount(str));
        encoding.GetBytes(str, 0, str.Length, bytes, 0);

        return bytes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBytesAscii(this string str, Span<byte> buffer) => Encoding.ASCII.GetBytes(str, buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBytesAscii(this ReadOnlySpan<char> str, Span<byte> buffer) => Encoding.ASCII.GetBytes(str, buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBytesBigUni(this string str, Span<byte> buffer) => Unicode.GetBytes(str, buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBytesBigUni(this ReadOnlySpan<char> str, Span<byte> buffer) => Unicode.GetBytes(str, buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBytesLittleUni(this string str, Span<byte> buffer) => UnicodeLE.GetBytes(str, buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBytesLittleUni(this ReadOnlySpan<char> str, Span<byte> buffer) => UnicodeLE.GetBytes(str, buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBytesUtf8(this string str, Span<byte> buffer) => UTF8.GetBytes(str, buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBytesUtf8(this ReadOnlySpan<char> str, Span<byte> buffer) => UTF8.GetBytes(str, buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetByteLengthForEncoding(this Encoding encoding) =>
        encoding.BodyName switch
        {
            "utf-16BE" => 2,
            "utf-16"   => 2,
            "utf-32BE" => 3,
            "utf-32"   => 3,
            _          => 1
        };

    public static string GetString(ReadOnlySpan<byte> span, Encoding encoding, bool safeString = false)
    {
        if (!safeString)
        {
            return encoding.GetString(span);
        }

        var charCount = encoding.GetMaxCharCount(span.Length);

        char[] rentedChars = null;
        Span<char> chars = charCount <= 256
            ? stackalloc char[charCount]
            : rentedChars = STArrayPool<char>.Shared.Rent(charCount);

        try
        {
            var length = encoding.GetChars(span, chars);
            chars = chars[..length];

            var index = chars.IndexOfAnyExceptInRange((char)0x20, (char)0xFFFD);
            if (index == -1)
            {
                return new string(chars);
            }

            using var sb = charCount <= 256
                ? new ValueStringBuilder(stackalloc char[charCount])
                : ValueStringBuilder.Create(charCount);

            while (index != -1)
            {
                sb.Append(chars[..index]);

                if (index + 1 < chars.Length)
                {
                    chars = chars[(index + 1)..];
                    index = chars.IndexOfAnyExceptInRange((char)0x20, (char)0xFFFD);
                }
                else
                {
                    index = -1;
                }
            }

            if (chars.Length > 0)
            {
                sb.Append(chars);
            }

            return sb.ToString();
        }
        finally
        {
            if (rentedChars != null)
            {
                STArrayPool<char>.Shared.Return(rentedChars);
            }
        }
    }
}
