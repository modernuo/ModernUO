using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Server.Tests.Maps;

public class TileMatrixUopOrderTests
{
    private const int BlockSize = 196;
    private const int BlocksPerChunk = 2;

    [Fact]
    public void OrderMapEntries_OrdersByChunkIndex()
    {
        var entries = new Dictionary<int, UOPEntry>
        {
            [2] = new(300, 10),
            [0] = new(100, 10),
            [1] = new(200, 10)
        };

        var ordered = TileMatrix.OrderMapEntries(entries);

        Assert.Equal(new long[] { 100, 200, 300 }, Array.ConvertAll(ordered, e => e.Offset));
    }

    // A map UOP whose chunks all start with the same int32 (as files from tools other than EA's can),
    // stored on disk in reverse order: each chunk must still resolve to its own data.
    [Theory]
    [InlineData(3)]
    [InlineData(40)]
    public void OrderMapEntries_IgnoresRepeatedChunkHeaders(int chunkCount)
    {
        var dir = Directory.CreateTempSubdirectory("muo-uop-order-");
        try
        {
            var path = Path.Combine(dir.FullName, "map96LegacyMUL.uop");
            WriteMapUop(path, chunkCount);

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var ordered = TileMatrix.OrderMapEntries(UOPFiles.ReadUOPIndexes(stream, ".dat", 0x14000, 5));

            Assert.Equal(chunkCount, ordered.Length);
            var reader = new BinaryReader(stream);
            for (var i = 0; i < chunkCount; i++)
            {
                stream.Seek(ordered[i].Offset + 4, SeekOrigin.Begin); // past the shared block header
                Assert.Equal(i, reader.ReadByte());
            }
        }
        finally
        {
            dir.Delete(true);
        }
    }

    private static void WriteMapUop(string path, int chunkCount)
    {
        const int headerSize = 4 + 4 + 4 + 8 + 4 + 4;
        const int entrySize = 8 + 4 + 4 + 4 + 8 + 4 + 2;
        const int chunkSize = BlockSize * BlocksPerChunk;
        var tableOffset = (long)headerSize;
        var dataOffset = tableOffset + 4 + 8 + chunkCount * entrySize;

        using var w = new BinaryWriter(File.Create(path));
        w.Write(0x50594Du);   // MYP\0
        w.Write(5);           // version
        w.Write(0xFD23EC43u); // signature
        w.Write(tableOffset);
        w.Write(chunkCount);  // block capacity
        w.Write(chunkCount);  // file count

        w.Write(chunkCount);
        w.Write(0L);          // no next table
        for (var i = chunkCount - 1; i >= 0; i--)
        {
            // Chunk i is stored at position (chunkCount - 1 - i): last chunk first.
            w.Write(dataOffset + (long)(chunkCount - 1 - i) * chunkSize);
            w.Write(0);         // header length
            w.Write(chunkSize); // compressed length
            w.Write(chunkSize); // decompressed length
            w.Write(UOPFiles.HashLittle2($"build/map96legacymul/{i:D8}.dat"));
            w.Write(0u);        // adler32
            w.Write((short)0);  // not compressed
        }

        for (var i = chunkCount - 1; i >= 0; i--)
        {
            var chunk = new byte[chunkSize];
            BitConverter.TryWriteBytes(chunk, 1); // every chunk's first block header is 1
            chunk.AsSpan(4).Fill((byte)i);
            w.Write(chunk);
        }
    }
}
