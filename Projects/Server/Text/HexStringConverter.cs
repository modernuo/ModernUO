/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: HexStringConverter.cs                                           *
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

namespace Server.Text;

public static class HexStringConverter
{
    public static readonly uint[] m_Lookup32Chars = CreateLookup32Chars();

    private static uint[] CreateLookup32Chars()
    {
        var result = new uint[256];
        for (var i = 0; i < 256; i++)
        {
            var s = i.ToString("X2");
            if (BitConverter.IsLittleEndian)
            {
                result[i] = s[0] + ((uint)s[1] << 16);
            }
            else
            {
                result[i] = s[1] + ((uint)s[0] << 16);
            }
        }

        return result;
    }

    public static string ToHexString(this byte[] bytes) => new ReadOnlySpan<byte>(bytes).ToHexString();

    public static string ToHexString(this Span<byte> bytes) => ((ReadOnlySpan<byte>)bytes).ToHexString();

    public static unsafe string ToHexString(this ReadOnlySpan<byte> bytes)
    {
        var result = new string((char)0, bytes.Length * 2);
        fixed (char* resultP = result)
        {
            var resultP2 = (uint*)resultP;
            for (var i = 0; i < bytes.Length; i++)
            {
                resultP2[i] = m_Lookup32Chars[bytes[i]];
            }
        }

        return result;
    }

    public static unsafe int ToSpacedHexString(this ReadOnlySpan<byte> bytes, Span<char> result)
    {
        var charsWritten = 0;
        fixed (char* resultP = result)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                var resultP2 = (uint*)(resultP + charsWritten);
                *resultP2 = m_Lookup32Chars[bytes[i]];
                charsWritten += 2;

                if (i < bytes.Length - 1)
                {
                    *(resultP + charsWritten++) = ' ';
                }
            }
        }

        return charsWritten;
    }

    public static string ToDelimitedHexString(this byte[] bytes) => ((ReadOnlySpan<byte>)bytes).ToDelimitedHexString();

    public static unsafe string ToDelimitedHexString(this ReadOnlySpan<byte> bytes)
    {
        const uint delimiter = 0x20002C; // ", "
        const char openBracket = '[';
        const char closeBracket = ']';
        var length = Math.Max(2, bytes.Length * 4); // len * 2 + (len - 1) * 2 + 2

        var result = new string((char)0, length);
        fixed (char* resultP = result)
        {
            resultP[0] = openBracket;
            resultP[length - 1] = closeBracket;

            var resultP2 = (uint*)(resultP + 1);
            for (int a = 0, i = 0; a < bytes.Length; a++, i++)
            {
                if (a > 0)
                {
                    resultP2[i++] = delimiter;
                }

                resultP2[i] = m_Lookup32Chars[bytes[a]];
            }
        }

        return result;
    }

    public static unsafe void GetBytes(this string str, Span<byte> bytes)
    {
        fixed (char* strP = str)
        {
            var i = 0;
            var j = 0;
            while (i < str.Length)
            {
                int chr1 = strP[i++];
                int chr2 = strP[i++];
                if (BitConverter.IsLittleEndian)
                {
                    bytes[j++] = (byte)(((chr1 - (chr1 >= 65 ? 55 : 48)) << 4) | (chr2 - (chr2 >= 65 ? 55 : 48)));
                }
                else
                {
                    bytes[j++] = (byte)((chr1 - (chr1 >= 65 ? 55 : 48)) | ((chr2 - (chr2 >= 65 ? 55 : 48)) << 4));
                }
            }
        }
    }
}
