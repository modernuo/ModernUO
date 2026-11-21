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
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
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

    // Cp1252 bytes a decode must remap to a typographic char (0x80-0x9F) or a safe decode must drop
    private static readonly SearchValues<byte> Cp1252RemapOrInvalidBytes = SearchValues.Create(
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
        0x7F,
        0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
        0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
        0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97,
        0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F
    );

    // Cp1252 bytes a safe decode drops: C0, DEL, and the five bytes Windows-1252 leaves unassigned
    private static readonly SearchValues<byte> InvalidCp1252Bytes = SearchValues.Create(
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
        0x7F, 0x81, 0x8D, 0x8F, 0x90, 0x9D
    );

    // Windows-1252 bytes 0x80-0x9F. The unassigned 0x81, 0x8D, 0x8F, 0x90 and 0x9D keep their C1 char.
    private static ReadOnlySpan<char> Cp1252C1Chars =>
        "\u20AC\u0081\u201A\u0192\u201E\u2026\u2020\u2021\u02C6\u2030\u0160\u2039\u0152\u008D\u017D\u008F" +
        "\u0090\u2018\u2019\u201C\u201D\u2022\u2013\u2014\u02DC\u2122\u0161\u203A\u0153\u009D\u017E\u0178";

    public static Encoding UTF8 => _utf8 ??= new UTF8Encoding(false, false);
    public static Encoding Unicode => _unicode ??= new UnicodeEncoding(true, false, false);
    public static Encoding UnicodeLE => _unicodeLE ??= new UnicodeEncoding(false, false, false);

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

    /// <summary>
    /// Encodes <paramref name="chars"/> as Windows-1252, the code page of the client's one-byte strings, and returns the
    /// byte count. One byte per code point, so <paramref name="destination"/> needs at most chars.Length bytes.
    /// U+0080-U+009F, chars outside Cp1252, and surrogates (a pair counts once) become '?'.
    /// </summary>
    public static int GetBytesCp1252(this ReadOnlySpan<char> chars, Span<byte> destination)
    {
        if (Ascii.FromUtf16(chars, destination, out var ascii) == OperationStatus.Done)
        {
            return ascii;
        }

        var n = ascii;

        for (var i = ascii; i < chars.Length; i++)
        {
            var c = chars[i];

            if (char.IsHighSurrogate(c) && i + 1 < chars.Length && char.IsLowSurrogate(chars[i + 1]))
            {
                i++;
                destination[n++] = (byte)'?';
            }
            else
            {
                destination[n++] = ToCp1252Byte(c);
            }
        }

        return n;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBytesCp1252(this string str, Span<byte> destination) =>
        GetBytesCp1252(str.AsSpan(), destination);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ToCp1252Byte(char c) =>
        c switch
        {
            < '\x80'  => (byte)c,
            <= '\x9F' => (byte)'?',
            <= '\xFF' => (byte)c,
            _         => ToCp1252Typographic(c)
        };

    private static byte ToCp1252Typographic(char c) =>
        c switch
        {
            '\u20AC' => 0x80,
            '\u201A' => 0x82,
            '\u0192' => 0x83,
            '\u201E' => 0x84,
            '\u2026' => 0x85,
            '\u2020' => 0x86,
            '\u2021' => 0x87,
            '\u02C6' => 0x88,
            '\u2030' => 0x89,
            '\u0160' => 0x8A,
            '\u2039' => 0x8B,
            '\u0152' => 0x8C,
            '\u017D' => 0x8E,
            '\u2018' => 0x91,
            '\u2019' => 0x92,
            '\u201C' => 0x93,
            '\u201D' => 0x94,
            '\u2022' => 0x95,
            '\u2013' => 0x96,
            '\u2014' => 0x97,
            '\u02DC' => 0x98,
            '\u2122' => 0x99,
            '\u0161' => 0x9A,
            '\u203A' => 0x9B,
            '\u0153' => 0x9C,
            '\u017E' => 0x9E,
            '\u0178' => 0x9F,
            _        => (byte)'?'
        };

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
        encoding switch
        {
            UnicodeEncoding => 2,
            UTF32Encoding   => 4,
            _               => 1
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

        return FilterSingleByte(bytes, index, InvalidAsciiBytes, Encoding.ASCII, cp1252: false);
    }

    /// <summary>
    /// Decodes a Windows-1252 byte span, the code page the client writes its one-byte strings in, so typographic chars
    /// at 0x80-0x9F (ellipsis, curly quotes, dashes, trademark, euro...) survive. In safe mode drops C0, DEL, and the
    /// five unassigned bytes (0x81, 0x8D, 0x8F, 0x90, 0x9D).
    /// </summary>
    public static string GetStringCp1252(ReadOnlySpan<byte> bytes, bool safeString = false)
    {
        if (bytes.IsEmpty)
        {
            return "";
        }

        var index = safeString
            ? bytes.IndexOfAny(Cp1252RemapOrInvalidBytes)
            : bytes.IndexOfAnyInRange((byte)0x80, (byte)0x9F);

        if (index == -1)
        {
            return Encoding.Latin1.GetString(bytes);
        }

        if (safeString)
        {
            var invalid = bytes[index..].IndexOfAny(InvalidCp1252Bytes);
            if (invalid != -1)
            {
                return FilterSingleByte(bytes, index + invalid, InvalidCp1252Bytes, Encoding.Latin1, cp1252: true);
            }
        }

        return string.Create(
            bytes.Length,
            bytes,
            static (chars, b) =>
            {
                Encoding.Latin1.GetChars(b, chars);
                PatchCp1252(chars);
            }
        );
    }

    // Cp1252 is Latin1 except at 0x80-0x9F, so decode as Latin1 and remap the C1 chars that widening left behind.
    private static void PatchCp1252(Span<char> chars)
    {
        var index = chars.IndexOfAnyInRange('\x80', '\x9F');

        while (index != -1)
        {
            chars[index] = Cp1252C1Chars[chars[index] - 0x80];
            chars = chars[(index + 1)..];
            index = chars.IndexOfAnyInRange('\x80', '\x9F');
        }
    }

    // ASCII and Latin1 (the base of Cp1252) decode one char per byte, so the kept segments decode straight into one buffer.
    private static string FilterSingleByte(
        ReadOnlySpan<byte> bytes, int firstInvalidIndex, SearchValues<byte> invalid, Encoding encoding, bool cp1252
    )
    {
        var maxLength = bytes.Length;

        char[] rentedChars = null;
        var chars = maxLength <= 256
            ? stackalloc char[maxLength]
            : rentedChars = STArrayPool<char>.Shared.Rent(maxLength);

        var length = 0;
        var index = firstInvalidIndex;

        while (index != -1)
        {
            if (index > 0)
            {
                length += encoding.GetChars(bytes[..index], chars[length..]);
            }

            bytes = bytes[(index + 1)..];
            index = bytes.IndexOfAny(invalid);
        }

        if (bytes.Length > 0)
        {
            length += encoding.GetChars(bytes, chars[length..]);
        }

        chars = chars[..length];

        if (cp1252)
        {
            PatchCp1252(chars);
        }

        var result = new string(chars);

        if (rentedChars != null)
        {
            STArrayPool<char>.Shared.Return(rentedChars);
        }

        return result;
    }

    /// <summary>
    /// Decodes a UTF-8 byte span to a string, filtering control codes and non-characters in safe mode.
    /// </summary>
    /// <remarks>
    /// Filters: C0 (0x00-0x1F), DEL (0x7F), C1 (0x80-0x9F), and non-characters (0xFFFE-0xFFFF).
    /// </remarks>
    public static string GetStringUtf8(ReadOnlySpan<byte> bytes, bool safeString = false) =>
        safeString ? FilterInvalidUnicodeChars(UTF8.GetString(bytes)) : UTF8.GetString(bytes);

    /// <summary>
    /// Decodes a big-endian UTF-16 byte span to a string, filtering control codes and non-characters in safe mode.
    /// </summary>
    public static string GetStringBigUni(ReadOnlySpan<byte> bytes, bool safeString = false) =>
        safeString ? FilterInvalidUnicodeChars(Unicode.GetString(bytes)) : Unicode.GetString(bytes);

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
        return index == -1 ? new string(chars) : FilterInvalidUnicodeChars(chars, index);
    }

    /// <summary>
    /// Generic string decoding with filtering. Prefer encoding-specific methods for better performance.
    /// </summary>
    public static string GetString(ReadOnlySpan<byte> span, Encoding encoding, bool safeString = false) =>
        safeString ? FilterInvalidUnicodeChars(encoding.GetString(span)) : encoding.GetString(span);

    /// <summary>
    /// Finds the first invalid Unicode character for display.
    /// Invalid: C0 (0x00-0x1F), DEL (0x7F), C1 (0x80-0x9F), non-chars (0xFFFE-0xFFFF).
    /// Note: Surrogate pairs (0xD800-0xDFFF) are not filtered - proper validation requires context checking.
    /// </summary>
    /// <remarks>
    /// One vectorized pass that tests all three ranges per block.
    /// </remarks>
    private static int IndexOfInvalidUnicodeChar(ReadOnlySpan<char> chars)
    {
        ref var src = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(chars));
        var length = chars.Length;

        if (Vector256.IsHardwareAccelerated && length >= Vector256<ushort>.Count)
        {
            var i = 0;

            for (; i <= length - 2 * Vector256<ushort>.Count; i += 2 * Vector256<ushort>.Count)
            {
                var invalid0 = InvalidUnicodeMask(Vector256.LoadUnsafe(ref src, (nuint)i));
                var invalid1 = InvalidUnicodeMask(Vector256.LoadUnsafe(ref src, (nuint)(i + Vector256<ushort>.Count)));

                if ((invalid0 | invalid1) != Vector256<ushort>.Zero)
                {
                    return invalid0 != Vector256<ushort>.Zero
                        ? i + BitOperations.TrailingZeroCount(invalid0.ExtractMostSignificantBits())
                        : i + Vector256<ushort>.Count +
                          BitOperations.TrailingZeroCount(invalid1.ExtractMostSignificantBits());
                }
            }

            if (i <= length - Vector256<ushort>.Count)
            {
                var invalid = InvalidUnicodeMask(Vector256.LoadUnsafe(ref src, (nuint)i));
                if (invalid != Vector256<ushort>.Zero)
                {
                    return i + BitOperations.TrailingZeroCount(invalid.ExtractMostSignificantBits());
                }

                i += Vector256<ushort>.Count;
            }

            if (i < length)
            {
                // The last block overlaps lanes already known to be valid, so its first hit is the first in the tail
                var start = length - Vector256<ushort>.Count;
                var invalid = InvalidUnicodeMask(Vector256.LoadUnsafe(ref src, (nuint)start));
                if (invalid != Vector256<ushort>.Zero)
                {
                    return start + BitOperations.TrailingZeroCount(invalid.ExtractMostSignificantBits());
                }
            }

            return -1;
        }

        if (Vector128.IsHardwareAccelerated && length >= Vector128<ushort>.Count)
        {
            var i = 0;

            for (; i <= length - Vector128<ushort>.Count; i += Vector128<ushort>.Count)
            {
                var invalid = InvalidUnicodeMask(Vector128.LoadUnsafe(ref src, (nuint)i));
                if (invalid != Vector128<ushort>.Zero)
                {
                    return i + BitOperations.TrailingZeroCount(invalid.ExtractMostSignificantBits());
                }
            }

            if (i < length)
            {
                var start = length - Vector128<ushort>.Count;
                var invalid = InvalidUnicodeMask(Vector128.LoadUnsafe(ref src, (nuint)start));
                if (invalid != Vector128<ushort>.Zero)
                {
                    return start + BitOperations.TrailingZeroCount(invalid.ExtractMostSignificantBits());
                }
            }

            return -1;
        }

        for (var i = 0; i < length; i++)
        {
            if (IsInvalidUnicodeChar(chars[i]))
            {
                return i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInvalidUnicodeChar(char c) => c < 0x20 || (uint)(c - 0x7F) <= 0x9F - 0x7F || c >= 0xFFFE;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<ushort> InvalidUnicodeMask(Vector256<ushort> v) =>
        Vector256.LessThan(v, Vector256.Create((ushort)0x20)) |
        Vector256.LessThanOrEqual(v - Vector256.Create((ushort)0x7F), Vector256.Create((ushort)(0x9F - 0x7F))) |
        Vector256.GreaterThanOrEqual(v, Vector256.Create((ushort)0xFFFE));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<ushort> InvalidUnicodeMask(Vector128<ushort> v) =>
        Vector128.LessThan(v, Vector128.Create((ushort)0x20)) |
        Vector128.LessThanOrEqual(v - Vector128.Create((ushort)0x7F), Vector128.Create((ushort)(0x9F - 0x7F))) |
        Vector128.GreaterThanOrEqual(v, Vector128.Create((ushort)0xFFFE));

    // Decoding straight into the string and scanning it leaves the clean (common) case with one allocation and no copy
    private static string FilterInvalidUnicodeChars(string str)
    {
        var index = IndexOfInvalidUnicodeChar(str);
        return index == -1 ? str : FilterInvalidUnicodeChars(str, index);
    }

    /// <summary>
    /// Filters invalid Unicode characters from a char span by removing them.
    /// The UO client renders nothing for invalid characters, so removal is most efficient.
    /// </summary>
    private static string FilterInvalidUnicodeChars(ReadOnlySpan<char> chars, int firstInvalidIndex)
    {
        var maxLength = chars.Length;

        char[] rentedChars = null;
        var buffer = maxLength <= 256
            ? stackalloc char[maxLength]
            : rentedChars = STArrayPool<char>.Shared.Rent(maxLength);

        var length = 0;
        var index = firstInvalidIndex;

        while (index != -1)
        {
            chars[..index].CopyTo(buffer[length..]);
            length += index;
            chars = chars[(index + 1)..];
            index = IndexOfInvalidUnicodeChar(chars);
        }

        chars.CopyTo(buffer[length..]);
        length += chars.Length;

        var result = new string(buffer[..length]);

        if (rentedChars != null)
        {
            STArrayPool<char>.Shared.Return(rentedChars);
        }

        return result;
    }
}
