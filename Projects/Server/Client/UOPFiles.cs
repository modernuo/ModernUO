/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
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

using System.Collections.Generic;
using System.IO;

namespace Server;

public static class UOPFiles
{
    public static Dictionary<int, UOPEntry> ReadUOPIndexes(FileStream stream, string fileExt, int entryCount = 0x14000, int expectedVersion = -1)
    {
        var reader = new BinaryReader(stream);
        var length = (int)stream.Length;

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

        var hashes = GenerateHashes(stream.Name, fileExt, entryCount);

        var nextBlock = reader.ReadInt64();

        var entries = new Dictionary<int, UOPEntry>(0x10000);

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

                if (offset != 0 && hashes.TryGetValue(fileNameHash, out var fileIndex) && compressedLength > 0 &&
                    decompressedLength > 0)
                {
                    entries[fileIndex] = new UOPEntry(offset + headerLength, decompressedLength)
                    {
                        Compressed = compressed,
                        CompressedSize = compressedLength
                    };
                }
            }
        }
        while (nextBlock != 0 && nextBlock < length);

        entries.TrimExcess();
        return entries;
    }

    private static Dictionary<ulong, int> GenerateHashes(string filePath, string ext, int entryCount)
    {
        var hashes = new Dictionary<ulong, int>();

        var root = $"build/{Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant()}";

        for (var i = 0; i < entryCount; i++)
        {
            hashes[UOPHash.HashLittle2($"{root}/{i:D8}{ext}")] = i;
        }

        return hashes;
    }
}
