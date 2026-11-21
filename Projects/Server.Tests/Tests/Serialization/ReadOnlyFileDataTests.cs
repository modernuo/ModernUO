using System;
using System.IO;
using Xunit;

namespace Server.Tests;

public class ReadOnlyFileDataTests
{
    private static string TempFile() => Path.Combine(Path.GetTempPath(), $"muo-rofd-{Guid.NewGuid():N}.bin");

    [Fact]
    public void ReaderIsBoundedByTheFileLengthNotThePage()
    {
        var path = TempFile();

        try
        {
            // 4099 bytes: not a page multiple, so a page-rounded view would leave readable padding.
            using (var writer = new FileBufferWriter(path))
            {
                writer.Write("payload");
                writer.Write(new byte[4099 - 10]);
                writer.Write((byte)0x5A);
            }

            using var data = new ReadOnlyFileData(path);
            Assert.Equal(4099, data.Length);

            var reader = data.CreateReader();
            Assert.Equal("payload", reader.ReadString());
            reader.Seek(4099 - 10, SeekOrigin.Current);
            Assert.Equal(0x5A, reader.ReadByte());

            Assert.Throws<EndOfStreamException>(() => reader.ReadByte());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void EmptyFileHasNoDataAndReadsThrow()
    {
        var path = TempFile();

        try
        {
            File.WriteAllBytes(path, []);

            using var data = new ReadOnlyFileData(path);
            Assert.Equal(0, data.Length);

            var reader = data.CreateReader();
            Assert.Throws<EndOfStreamException>(() => reader.ReadByte());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
