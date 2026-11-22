using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Server.Tests.Maps;

public class TileMatrixUopOrderTests
{
    private const int ChunkSize = 196 * 2;

    // Chunks whose first int32 repeats (as files from tools other than EA's can), stored on disk in reverse:
    // each chunk must still be placed by the index in its name.
    [Theory]
    [InlineData(3)]
    [InlineData(40)]
    public void BuildMapChunks_IgnoresRepeatedChunkHeaders(int chunkCount)
    {
        var indexes = new int[chunkCount];
        for (var i = 0; i < chunkCount; i++)
        {
            indexes[i] = chunkCount - 1 - i;
        }

        AssertChunksResolve(indexes, chunkCount);
    }

    [Fact]
    public void BuildMapChunks_MissingChunkLeavesHole()
    {
        AssertChunksResolve([0, 1, 3, 4], 5);
    }

    [Fact]
    public void ReadUOPIndexes_FindsAdditionalHashes()
    {
        var dir = Directory.CreateTempSubdirectory("muo-uop-order-");
        try
        {
            var path = Path.Combine(dir.FullName, "map96LegacyMUL.uop");
            var extraHash = UOPFiles.HashLittle2("build/map96legacymul/extra.bin");
            WriteMapUop(path, [1, 0], extraHash);

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var additional = new Dictionary<ulong, UOPEntry> { [extraHash] = default };
            var entries = UOPFiles.ReadUOPIndexes(stream, ".dat", 0x14000, 5, 8, additional);

            Assert.Equal(2, entries.Count);
            Assert.True(entries.ContainsKey(0));
            Assert.True(entries.ContainsKey(1));
            Assert.NotEqual(0, additional[extraHash].Offset);
        }
        finally
        {
            dir.Delete(true);
        }
    }

    private static void AssertChunksResolve(int[] storedIndexes, int expectedLength)
    {
        var dir = Directory.CreateTempSubdirectory("muo-uop-order-");
        try
        {
            var path = Path.Combine(dir.FullName, "map96LegacyMUL.uop");
            WriteMapUop(path, storedIndexes);

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var chunks = TileMatrix.BuildMapChunks(UOPFiles.ReadUOPIndexes(stream, ".dat", 0x14000, 5));

            Assert.Equal(expectedLength, chunks.Length);
            var present = new HashSet<int>(storedIndexes);
            for (var i = 0; i < chunks.Length; i++)
            {
                if (!present.Contains(i))
                {
                    Assert.Equal(0, chunks[i].Offset);
                    continue;
                }

                stream.Seek(chunks[i].Offset + 4, SeekOrigin.Begin); // past the shared block header
                Assert.Equal(i, stream.ReadByte());
            }
        }
        finally
        {
            dir.Delete(true);
        }
    }

    // Writes chunk storedIndexes[n] at storage position n. Every chunk starts with the same int32 (1)
    // and is filled with its own index after that.
    private static void WriteMapUop(string path, int[] storedIndexes, ulong extraHash = 0)
    {
        const int headerSize = 4 + 4 + 4 + 8 + 4 + 4;
        const int entrySize = 8 + 4 + 4 + 4 + 8 + 4 + 2;
        var fileCount = storedIndexes.Length + (extraHash != 0 ? 1 : 0);
        var tableOffset = (long)headerSize;
        var dataOffset = tableOffset + 4 + 8 + fileCount * entrySize;

        using var w = new BinaryWriter(File.Create(path));
        w.Write(0x50594Du);   // MYP\0
        w.Write(5);           // version
        w.Write(0xFD23EC43u); // signature
        w.Write(tableOffset);
        w.Write(fileCount);   // block capacity
        w.Write(fileCount);   // file count

        w.Write(fileCount);
        w.Write(0L);          // no next table
        for (var n = 0; n < fileCount; n++)
        {
            var hash = n < storedIndexes.Length
                ? UOPFiles.HashLittle2($"build/map96legacymul/{storedIndexes[n]:D8}.dat")
                : extraHash;

            w.Write(dataOffset + (long)n * ChunkSize);
            w.Write(0);         // header length
            w.Write(ChunkSize); // compressed length
            w.Write(ChunkSize); // decompressed length
            w.Write(hash);
            w.Write(0u);        // adler32
            w.Write((short)0);  // not compressed
        }

        for (var n = 0; n < fileCount; n++)
        {
            var chunk = new byte[ChunkSize];
            BitConverter.TryWriteBytes(chunk, 1);
            chunk.AsSpan(4).Fill(n < storedIndexes.Length ? (byte)storedIndexes[n] : (byte)0xFF);
            w.Write(chunk);
        }
    }
}
