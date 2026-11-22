/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: UOPFiles.cs                                                     *
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
using System.IO;

namespace Server;

public static class UOPFiles
{
    public static Dictionary<int, UOPEntry> ReadUOPIndexes(
        FileStream stream, string fileExt, int entryCount = 0x14000, int expectedVersion = -1, int precision = 8,
        Dictionary<ulong, UOPEntry> additionalHashes = null
    )
    {
        var reader = new BinaryReader(stream);
        var length = stream.Length;

        // MYP\0
        if (reader.ReadUInt32() != 0x50594D)
        {
            throw new FileLoadException($"Error loading file {stream.Name}. The file is not a UOP file.");
        }

        var version = reader.ReadInt32();
        if (expectedVersion > -1 && version != expectedVersion)
        {
            throw new FileLoadException($"Error loading file {stream.Name}. Expected version {expectedVersion}.");
        }

        // Signature
        if (reader.ReadUInt32() != 0xFD23EC43)
        {
            throw new FileLoadException($"Error loading file {stream.Name}. Invalid signature.");
        }

        var nextBlock = reader.ReadInt64();

        // The table is keyed by name hash, so read it whole and then hash candidate names against it.
        var tableEntries = new Dictionary<ulong, UOPEntry>();

        do
        {
            stream.Seek(nextBlock, SeekOrigin.Begin);
            var fileCount = reader.ReadInt32();
            nextBlock = reader.ReadInt64();

            for (var i = 0; i < fileCount; ++i)
            {
                var offset = reader.ReadInt64();
                var headerLength = reader.ReadInt32();
                var compressedLength = reader.ReadInt32();
                var decompressedLength = reader.ReadInt32();
                var fileNameHash = reader.ReadUInt64();

                reader.ReadUInt32(); // Adler32

                var compressed = reader.ReadInt16() == 1;

                if (offset == 0 || compressedLength <= 0 || decompressedLength <= 0)
                {
                    continue;
                }

                tableEntries[fileNameHash] = new UOPEntry(offset + headerLength, decompressedLength)
                {
                    Compressed = compressed,
                    CompressedSize = compressedLength
                };
            }
        }
        while (nextBlock != 0 && nextBlock < length);

        if (additionalHashes != null)
        {
            foreach (var hash in additionalHashes.Keys)
            {
                if (tableEntries.Remove(hash, out var entry))
                {
                    additionalHashes[hash] = entry;
                }
            }
        }

        var entries = new Dictionary<int, UOPEntry>(Math.Min(tableEntries.Count, entryCount));

        // build/<file name>/<index, zero-padded to precision><ext>
        var root = Path.GetFileNameWithoutExtension(stream.Name);
        var nameLength = 6 + root.Length + 1 + precision + fileExt.Length;
        Span<char> name = nameLength <= 256 ? stackalloc char[nameLength] : new char[nameLength];
        "build/".CopyTo(name);
        root.AsSpan().ToLowerInvariant(name[6..]);
        name[6 + root.Length] = '/';
        fileExt.CopyTo(name[^fileExt.Length..]);
        var digits = name.Slice(6 + root.Length + 1, precision);

        // Stops once every table entry is placed, so a map hashes ~100 names instead of entryCount.
        for (var i = 0; i < entryCount && tableEntries.Count > 0; i++)
        {
            var n = i;
            for (var d = precision - 1; d >= 0; d--)
            {
                digits[d] = (char)('0' + n % 10);
                n /= 10;
            }

            if (tableEntries.Remove(HashLittle2(name), out var entry))
            {
                entries[i] = entry;
            }
        }

        return entries;
    }

    public static ulong HashLittle2(ReadOnlySpan<char> s)
    {
        var length = s.Length;

        uint b, c;
        var a = b = c = 0xDEADBEEF + (uint)length;

        var k = 0;

        while (length > 12)
        {
            a += s[k];
            a += (uint)s[k + 1] << 8;
            a += (uint)s[k + 2] << 16;
            a += (uint)s[k + 3] << 24;
            b += s[k + 4];
            b += (uint)s[k + 5] << 8;
            b += (uint)s[k + 6] << 16;
            b += (uint)s[k + 7] << 24;
            c += s[k + 8];
            c += (uint)s[k + 9] << 8;
            c += (uint)s[k + 10] << 16;
            c += (uint)s[k + 11] << 24;

            a -= c;
            a ^= (c << 4) | (c >> 28);
            c += b;
            b -= a;
            b ^= (a << 6) | (a >> 26);
            a += c;
            c -= b;
            c ^= (b << 8) | (b >> 24);
            b += a;
            a -= c;
            a ^= (c << 16) | (c >> 16);
            c += b;
            b -= a;
            b ^= (a << 19) | (a >> 13);
            a += c;
            c -= b;
            c ^= (b << 4) | (b >> 28);
            b += a;

            length -= 12;
            k += 12;
        }

        if (length != 0)
        {
            switch (length)
            {
                case 12:
                    {
                        c += (uint)s[k + 11] << 24;
                        goto case 11;
                    }
                case 11:
                    {
                        c += (uint)s[k + 10] << 16;
                        goto case 10;
                    }
                case 10:
                    {
                        c += (uint)s[k + 9] << 8;
                        goto case 9;
                    }
                case 9:
                    {
                        c += s[k + 8];
                        goto case 8;
                    }
                case 8:
                    {
                        b += (uint)s[k + 7] << 24;
                        goto case 7;
                    }
                case 7:
                    {
                        b += (uint)s[k + 6] << 16;
                        goto case 6;
                    }
                case 6:
                    {
                        b += (uint)s[k + 5] << 8;
                        goto case 5;
                    }
                case 5:
                    {
                        b += s[k + 4];
                        goto case 4;
                    }
                case 4:
                    {
                        a += (uint)s[k + 3] << 24;
                        goto case 3;
                    }
                case 3:
                    {
                        a += (uint)s[k + 2] << 16;
                        goto case 2;
                    }
                case 2:
                    {
                        a += (uint)s[k + 1] << 8;
                        goto case 1;
                    }
                case 1:
                    {
                        a += s[k];
                        break;
                    }
            }

            c ^= b;
            c -= (b << 14) | (b >> 18);
            a ^= c;
            a -= (c << 11) | (c >> 21);
            b ^= a;
            b -= (a << 25) | (a >> 7);
            c ^= b;
            c -= (b << 16) | (b >> 16);
            a ^= c;
            a -= (c << 4) | (c >> 28);
            b ^= a;
            b -= (a << 14) | (a >> 18);
            c ^= b;
            c -= (b << 24) | (b >> 8);
        }

        return ((ulong)b << 32) | c;
    }
}
