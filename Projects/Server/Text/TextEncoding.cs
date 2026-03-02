/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Server.Buffers;

namespace Server.Text;

public static class TextEncoding
{
    private static Encoding _utf8, _unicode, _unicodeLE;

    // SearchValues for invalid ASCII display bytes (C0: 0x00-0x1F, DEL: 0x7F)
    // Note: Bytes >= 0x80 are invalid ASCII and become '?' when decoded, which is acceptable
    private static readonly SearchValues<byte> InvalidAsciiBytes = SearchValues.Create(
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
        0x7F
    );

    // SearchValues for invalid Latin1 bytes (C0: 0x00-0x1F, C1: 0x80-0x9F)
    private static readonly SearchValues<byte> InvalidLatin1Bytes = SearchValues.Create(
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
        0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
        0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
        0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97,
        0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F
    );

    // SearchValues for invalid Latin1 chars (same ranges as bytes)
    private static readonly SearchValues<char> InvalidLatin1Chars = SearchValues.Create(
        '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\x07',
        '\x08', '\x09', '\x0A', '\x0B', '\x0C', '\x0D', '\x0E', '\x0F',
        '\x10', '\x11', '\x12', '\x13', '\x14', '\x15', '\x16', '\x17',
        '\x18', '\x19', '\x1A', '\x1B', '\x1C', '\x1D', '\x1E', '\x1F',
        '\x80', '\x81', '\x82', '\x83', '\x84', '\x85', '\x86', '\x87',
        '\x88', '\x89', '\x8A', '\x8B', '\x8C', '\x8D', '\x8E', '\x8F',
        '\x90', '\x91', '\x92', '\x93', '\x94', '\x95', '\x96', '\x97',
        '\x98', '\x99', '\x9A', '\x9B', '\x9C', '\x9D', '\x9E', '\x9F'
    );

    public static Encoding UTF8 => _utf8 ??= new UTF8Encoding(false, false);
    public static Encoding Unicode => _unicode ??= new UnicodeEncoding(true, false, false);
    public static Encoding UnicodeLE => _unicodeLE ??= new UnicodeEncoding(false, false, false);
    public static Encoding Latin1 => Encoding.Latin1;

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
    public static byte[] GetBytesLatin1(this ReadOnlySpan<char> str) => GetBytes(str, Latin1);

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
    public static int GetBytesLatin1(this ReadOnlySpan<char> str, Span<byte> buffer) => Latin1.GetBytes(str, buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBytesLatin1(this string str, Span<byte> buffer) => Latin1.GetBytes(str, buffer);

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

    /// <summary>
    /// Decodes an ASCII byte span to a string, filtering C0 control codes and DEL in safe mode.
    /// </summary>
    /// <remarks>
    /// For ASCII, bytes >= 0x80 are invalid and become '?' when decoded, which is acceptable for display.
    /// Only C0 (0x00-0x1F) and DEL (0x7F) need explicit filtering.
    /// </remarks>
    public static string GetStringAscii(ReadOnlySpan<byte> bytes, bool safeString = false)
    {
        if (!safeString)
        {
            return Encoding.ASCII.GetString(bytes);
        }

        // Check bytes directly - filter at byte level, then decode once
        var index = bytes.IndexOfAny(InvalidAsciiBytes);
        if (index == -1)
        {
            return Encoding.ASCII.GetString(bytes);
        }

        // Has invalid bytes, filter and decode
        return FilterInvalidAsciiBytes(bytes, index);
    }

    private static string FilterInvalidAsciiBytes(ReadOnlySpan<byte> bytes, int firstInvalidIndex)
    {
        var maxLength = bytes.Length;

        char[] rentedChars = null;
        var charBuffer = maxLength <= 256
            ? stackalloc char[maxLength]
            : rentedChars = STArrayPool<char>.Shared.Rent(maxLength);

        try
        {
            using var sb = maxLength <= 256
                ? new ValueStringBuilder(stackalloc char[maxLength])
                : ValueStringBuilder.Create(maxLength);

            var index = firstInvalidIndex;

            while (index != -1)
            {
                if (index > 0)
                {
                    // Decode segment and append
                    var segment = bytes[..index];
                    var decoded = Encoding.ASCII.GetChars(segment, charBuffer);
                    sb.Append(charBuffer[..decoded]);
                }

                if (index + 1 < bytes.Length)
                {
                    bytes = bytes[(index + 1)..];
                    index = bytes.IndexOfAny(InvalidAsciiBytes);
                }
                else
                {
                    bytes = [];
                    index = -1;
                }
            }

            if (bytes.Length > 0)
            {
                var decoded = Encoding.ASCII.GetChars(bytes, charBuffer);
                sb.Append(charBuffer[..decoded]);
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

    /// <summary>
    /// Decodes a Latin1 byte span to a string, filtering C0/C1 control codes in safe mode.
    /// </summary>
    public static string GetStringLatin1(ReadOnlySpan<byte> bytes, bool safeString = false)
    {
        if (!safeString)
        {
            return Latin1.GetString(bytes);
        }

        // Check bytes directly - Latin1 is 1:1 mapping, avoids char buffer for valid strings
        var index = bytes.IndexOfAny(InvalidLatin1Bytes);
        if (index == -1)
        {
            return Latin1.GetString(bytes);
        }

        // Has invalid bytes, need to filter - now allocate char buffer
        return FilterInvalidLatin1Chars(bytes, index);
    }

    private static string FilterInvalidLatin1Chars(ReadOnlySpan<byte> bytes, int firstInvalidIndex)
    {
        var charCount = bytes.Length;

        char[] rentedChars = null;
        var chars = charCount <= 256
            ? stackalloc char[charCount]
            : rentedChars = STArrayPool<char>.Shared.Rent(charCount);

        try
        {
            var length = Latin1.GetChars(bytes, chars);
            chars = chars[..length];

            using var sb = charCount <= 256
                ? new ValueStringBuilder(stackalloc char[charCount])
                : ValueStringBuilder.Create(charCount);

            // Start from the first invalid index we already found
            var index = firstInvalidIndex;

            while (index != -1)
            {
                sb.Append(chars[..index]);

                if (index + 1 < chars.Length)
                {
                    chars = chars[(index + 1)..];
                    index = chars.IndexOfAny(InvalidLatin1Chars);
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

    /// <summary>
    /// Decodes a UTF-8 byte span to a string, filtering control codes and non-characters in safe mode.
    /// </summary>
    /// <remarks>
    /// Filters: C0 (0x00-0x1F), DEL (0x7F), C1 (0x80-0x9F), and non-characters (0xFFFD-0xFFFF).
    /// Optimized to check bytes for C0/DEL first (single-byte in UTF-8), then chars for C1/non-chars.
    /// </remarks>
    public static string GetStringUtf8(ReadOnlySpan<byte> bytes, bool safeString = false)
    {
        if (!safeString)
        {
            return UTF8.GetString(bytes);
        }

        // Quick check: C0 (0x00-0x1F) and DEL (0x7F) are single-byte in UTF-8
        var hasC0OrDel = bytes.IndexOfAny(InvalidAsciiBytes) >= 0;

        var charCount = UTF8.GetMaxCharCount(bytes.Length);

        char[] rentedChars = null;
        var chars = charCount <= 256
            ? stackalloc char[charCount]
            : rentedChars = STArrayPool<char>.Shared.Rent(charCount);

        try
        {
            var length = UTF8.GetChars(bytes, chars);
            chars = chars[..length];

            // If no C0/DEL in bytes, only need to check for C1 and non-chars
            var index = hasC0OrDel
                ? IndexOfInvalidUnicodeChar(chars)
                : IndexOfInvalidUnicodeCharNonAscii(chars);

            if (index == -1)
            {
                return new string(chars);
            }

            return FilterInvalidUnicodeChars(chars, index, hasC0OrDel);
        }
        finally
        {
            if (rentedChars != null)
            {
                STArrayPool<char>.Shared.Return(rentedChars);
            }
        }
    }

    /// <summary>
    /// Decodes a big-endian UTF-16 byte span to a string, filtering control codes and non-characters in safe mode.
    /// </summary>
    public static string GetStringBigUni(ReadOnlySpan<byte> bytes, bool safeString = false)
    {
        if (!safeString)
        {
            return Unicode.GetString(bytes);
        }

        var charCount = Unicode.GetMaxCharCount(bytes.Length);

        char[] rentedChars = null;
        var chars = charCount <= 256
            ? stackalloc char[charCount]
            : rentedChars = STArrayPool<char>.Shared.Rent(charCount);

        try
        {
            var length = Unicode.GetChars(bytes, chars);
            chars = chars[..length];

            var index = IndexOfInvalidUnicodeChar(chars);
            if (index == -1)
            {
                return new string(chars);
            }

            return FilterInvalidUnicodeChars(chars, index, fullCheck: true);
        }
        finally
        {
            if (rentedChars != null)
            {
                STArrayPool<char>.Shared.Return(rentedChars);
            }
        }
    }

    /// <summary>
    /// Decodes a little-endian UTF-16 byte span to a string, filtering control codes and non-characters in safe mode.
    /// </summary>
    /// <remarks>
    /// Optimized for little-endian systems using direct memory cast (no decoding overhead).
    /// </remarks>
    public static string GetStringLittleUni(ReadOnlySpan<byte> bytes, bool safeString = false)
    {
        // Direct cast - UTF-16 LE bytes map directly to chars on little-endian systems
        var chars = MemoryMarshal.Cast<byte, char>(bytes);

        if (!safeString)
        {
            return new string(chars);
        }

        var index = IndexOfInvalidUnicodeChar(chars);
        if (index == -1)
        {
            return new string(chars);
        }

        return FilterInvalidUnicodeChars(chars, index, fullCheck: true);
    }

    /// <summary>
    /// Generic string decoding with filtering. Prefer encoding-specific methods for better performance.
    /// </summary>
    public static string GetString(ReadOnlySpan<byte> span, Encoding encoding, bool safeString = false)
    {
        if (!safeString)
        {
            return encoding.GetString(span);
        }

        var charCount = encoding.GetMaxCharCount(span.Length);

        char[] rentedChars = null;
        var chars = charCount <= 256
            ? stackalloc char[charCount]
            : rentedChars = STArrayPool<char>.Shared.Rent(charCount);

        try
        {
            var length = encoding.GetChars(span, chars);
            chars = chars[..length];

            var index = IndexOfInvalidUnicodeChar(chars);
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
                    index = IndexOfInvalidUnicodeChar(chars);
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

    /// <summary>
    /// Finds the first invalid Unicode character for display.
    /// Invalid: C0 (0x00-0x1F), DEL (0x7F), C1 (0x80-0x9F), non-chars (0xFFFE-0xFFFF).
    /// Note: Surrogate pairs (0xD800-0xDFFF) are not filtered - proper validation requires context checking.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfInvalidUnicodeChar(ReadOnlySpan<char> chars)
    {
        // Check invalid ranges and return the minimum index found
        var c0Index = chars.IndexOfAnyInRange((char)0x00, (char)0x1F);        // C0 control codes
        var delC1Index = chars.IndexOfAnyInRange((char)0x7F, (char)0x9F);     // DEL + C1 control codes
        var nonCharIndex = chars.IndexOfAnyInRange((char)0xFFFE, (char)0xFFFF); // Non-characters

        // Find minimum non-negative index
        var minIndex = -1;

        if (c0Index >= 0)
        {
            minIndex = c0Index;
        }

        if (delC1Index >= 0 && (minIndex < 0 || delC1Index < minIndex))
        {
            minIndex = delC1Index;
        }

        if (nonCharIndex >= 0 && (minIndex < 0 || nonCharIndex < minIndex))
        {
            minIndex = nonCharIndex;
        }

        return minIndex;
    }

    /// <summary>
    /// Finds the first invalid Unicode character, excluding C0/DEL (already checked at byte level).
    /// Checks: C1 (0x80-0x9F), non-chars (0xFFFE-0xFFFF).
    /// Note: Surrogate pairs (0xD800-0xDFFF) are not filtered - proper validation requires context checking.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfInvalidUnicodeCharNonAscii(ReadOnlySpan<char> chars)
    {
        // Only check C1 and non-chars (C0/DEL already verified absent at byte level)
        var c1Index = chars.IndexOfAnyInRange((char)0x80, (char)0x9F);           // C1 control codes
        var nonCharIndex = chars.IndexOfAnyInRange((char)0xFFFE, (char)0xFFFF);  // Non-characters

        if (c1Index < 0)
        {
            return nonCharIndex;
        }

        if (nonCharIndex < 0)
        {
            return c1Index;
        }

        return Math.Min(c1Index, nonCharIndex);
    }

    /// <summary>
    /// Filters invalid Unicode characters from a char span by removing them.
    /// The UO client renders nothing for invalid characters, so removal is most efficient.
    /// </summary>
    private static string FilterInvalidUnicodeChars(ReadOnlySpan<char> chars, int firstInvalidIndex, bool fullCheck)
    {
        var maxLength = chars.Length;

        using var sb = maxLength <= 256
            ? new ValueStringBuilder(stackalloc char[maxLength])
            : ValueStringBuilder.Create(maxLength);

        var index = firstInvalidIndex;

        while (index != -1)
        {
            sb.Append(chars[..index]);
            // Skip the invalid character (don't append replacement - client renders nothing anyway)

            if (index + 1 < chars.Length)
            {
                chars = chars[(index + 1)..];
                index = fullCheck
                    ? IndexOfInvalidUnicodeChar(chars)
                    : IndexOfInvalidUnicodeCharNonAscii(chars);
            }
            else
            {
                chars = [];
                index = -1;
            }
        }

        if (chars.Length > 0)
        {
            sb.Append(chars);
        }

        return sb.ToString();
    }
}
