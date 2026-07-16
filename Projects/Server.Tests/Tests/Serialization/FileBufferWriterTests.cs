using System;
using System.IO;
using System.Text;
using Xunit;

namespace Server.Tests;

public class FileBufferWriterTests
{
    private static string TempFile() =>
        Path.Combine(Path.GetTempPath(), $"muo-fbw-{Guid.NewGuid():N}.bin");

    [Fact]
    public void DrainsAcrossStagingBoundariesAndPatchesBackwards()
    {
        var path = TempFile();

        try
        {
            // Tiny staging block so every few records cross a drain; mirrors the idx
            // pattern: version, count placeholder, records, backwards count patch.
            using (var writer = new FileBufferWriter(path, expectedSize: 64))
            {
                writer.Write(3); // version

                var countPosition = writer.Position;
                writer.Write(0);

                const int records = 1000;
                for (var i = 0; i < records; i++)
                {
                    writer.Write((ulong)i * 0x9E3779B97F4A7C15);
                    writer.Write(i);
                }

                var end = writer.Position;
                writer.Seek(countPosition, SeekOrigin.Begin);
                writer.Write(records);
                writer.Seek(0, SeekOrigin.End);

                Assert.Equal(end, writer.Position);
            }

            var bytes = File.ReadAllBytes(path);
            Assert.Equal(4 + 4 + 1000 * 12, bytes.Length);

            IGenericReader reader = new BufferReader(bytes);
            Assert.Equal(3, reader.ReadInt());
            Assert.Equal(1000, reader.ReadInt());

            for (var i = 0; i < 1000; i++)
            {
                Assert.Equal((ulong)i * 0x9E3779B97F4A7C15, reader.ReadULong());
                Assert.Equal(i, reader.ReadInt());
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void OversizedSingleItemGrowsTheStagingBlock()
    {
        var path = TempFile();

        try
        {
            // A string whose worst-case reservation exceeds the staging block must grow
            // the block instead of deadlocking the drain loop.
            var value = new string('二', 500); // 1500 bytes utf8, staging 64

            using (var writer = new FileBufferWriter(path, expectedSize: 64))
            {
                writer.Write(value);
                writer.Write(0xC0FFEE);
            }

            IGenericReader reader = new BufferReader(File.ReadAllBytes(path));
            Assert.Equal(value, reader.ReadString());
            Assert.Equal(0xC0FFEE, reader.ReadInt());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SpanWritesCrossDrains()
    {
        var path = TempFile();

        try
        {
            var payload = new byte[777];
            new System.Random(0x5EED).NextBytes(payload);

            using (var writer = new FileBufferWriter(path, expectedSize: 64))
            {
                writer.Write((ReadOnlySpan<byte>)payload);
            }

            Assert.Equal(payload, File.ReadAllBytes(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TypeWritesUseTagAndHashFormat()
    {
        var path = TempFile();

        try
        {
            using (var writer = new FileBufferWriter(path))
            {
                writer.Write(typeof(string));
                writer.Write((Type)null);
            }

            var bytes = File.ReadAllBytes(path);
            Assert.Equal(1 + 8 + 1, bytes.Length); // flag + hash + null flag
            Assert.Equal(2, bytes[0]);
            Assert.Equal(0, bytes[^1]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
