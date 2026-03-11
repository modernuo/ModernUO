using System;
using System.Buffers;
using System.IO;
using Xunit;

namespace Server.Tests.Buffers;

public class SpanReaderTests
{
    [Fact]
    public void TestReadByte()
    {
        ReadOnlySpan<byte> buffer = [0x12, 0x34];
        var reader = new SpanReader(buffer);

        Assert.Equal(0x12, reader.ReadByte());
        Assert.Equal(0x34, reader.ReadByte());
        Assert.Equal(2, reader.Position);
    }

    [Fact]
    public void TestReadByteAtEnd()
    {
        Assert.Throws<EndOfStreamException>(
            () =>
            {
                ReadOnlySpan<byte> buffer = [0x12];
                var reader = new SpanReader(buffer);
                reader.ReadByte();
                reader.ReadByte();
            }
        );
    }

    [Fact]
    public void TestReadBoolean()
    {
        ReadOnlySpan<byte> buffer = [0, 1, 2, 255];
        var reader = new SpanReader(buffer);

        Assert.False(reader.ReadBoolean());
        Assert.True(reader.ReadBoolean());
        Assert.True(reader.ReadBoolean());
        Assert.True(reader.ReadBoolean());
        Assert.Equal(4, reader.Position);
    }

    [Fact]
    public void TestReadSByte()
    {
        ReadOnlySpan<byte> buffer = [0xFF, 0x7F];
        var reader = new SpanReader(buffer);

        Assert.Equal(-1, reader.ReadSByte());
        Assert.Equal(127, reader.ReadSByte());
        Assert.Equal(2, reader.Position);
    }

    [Fact]
    public void TestReadInt16BigEndian()
    {
        ReadOnlySpan<byte> buffer = [0x12, 0x34];
        var reader = new SpanReader(buffer);

        Assert.Equal(0x1234, reader.ReadInt16());
        Assert.Equal(2, reader.Position);
    }

    [Fact]
    public void TestReadInt16LittleEndian()
    {
        ReadOnlySpan<byte> buffer = [0x34, 0x12];
        var reader = new SpanReader(buffer);

        Assert.Equal(0x1234, reader.ReadInt16LE());
        Assert.Equal(2, reader.Position);
    }

    [Fact]
    public void TestReadInt16AtEnd()
    {
        Assert.Throws<EndOfStreamException>(
            () =>
            {
                ReadOnlySpan<byte> buffer = [0x12];
                var reader = new SpanReader(buffer);
                reader.ReadInt16();
            }
        );
    }

    [Fact]
    public void TestReadUInt16BigEndian()
    {
        ReadOnlySpan<byte> buffer = [0x12, 0x34];
        var reader = new SpanReader(buffer);

        Assert.Equal((ushort)0x1234, reader.ReadUInt16());
        Assert.Equal(2, reader.Position);
    }

    [Fact]
    public void TestReadUInt16LittleEndian()
    {
        ReadOnlySpan<byte> buffer = [0x34, 0x12];
        var reader = new SpanReader(buffer);

        Assert.Equal((ushort)0x1234, reader.ReadUInt16LE());
        Assert.Equal(2, reader.Position);
    }

    [Fact]
    public void TestReadInt32BigEndian()
    {
        ReadOnlySpan<byte> buffer = [0x12, 0x34, 0x56, 0x78];
        var reader = new SpanReader(buffer);

        Assert.Equal(0x12345678, reader.ReadInt32());
        Assert.Equal(4, reader.Position);
    }

    [Fact]
    public void TestReadInt32AtEnd()
    {
        Assert.Throws<EndOfStreamException>(
            () =>
            {
                ReadOnlySpan<byte> buffer = [0x12, 0x34, 0x56];
                var reader = new SpanReader(buffer);
                reader.ReadInt32();
            }
        );
    }

    [Fact]
    public void TestReadUInt32BigEndian()
    {
        ReadOnlySpan<byte> buffer = [0x12, 0x34, 0x56, 0x78];
        var reader = new SpanReader(buffer);

        Assert.Equal(0x12345678u, reader.ReadUInt32());
        Assert.Equal(4, reader.Position);
    }

    [Fact]
    public void TestReadUInt32LittleEndian()
    {
        ReadOnlySpan<byte> buffer = [0x78, 0x56, 0x34, 0x12];
        var reader = new SpanReader(buffer);

        Assert.Equal(0x12345678u, reader.ReadUInt32LE());
        Assert.Equal(4, reader.Position);
    }

    [Fact]
    public void TestReadInt64BigEndian()
    {
        ReadOnlySpan<byte> buffer = [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0];
        var reader = new SpanReader(buffer);

        Assert.Equal(0x123456789ABCDEF0L, reader.ReadInt64());
        Assert.Equal(8, reader.Position);
    }

    [Fact]
    public void TestReadInt64AtEnd()
    {
        Assert.Throws<EndOfStreamException>(
            () =>
            {
                ReadOnlySpan<byte> buffer = [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE];
                var reader = new SpanReader(buffer);
                reader.ReadInt64();
            }
        );
    }

    [Fact]
    public void TestReadUInt64BigEndian()
    {
        ReadOnlySpan<byte> buffer = [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0];
        var reader = new SpanReader(buffer);

        Assert.Equal(0x123456789ABCDEF0UL, reader.ReadUInt64());
        Assert.Equal(8, reader.Position);
    }

    [Fact]
    public void TestReadAsciiString()
    {
        var buffer = "Hello"u8;
        var reader = new SpanReader(buffer);

        var result = reader.ReadAscii();

        Assert.Equal("Hello", result);
        Assert.Equal(5, reader.Position);
    }

    [Fact]
    public void TestReadAsciiStringWithNull()
    {
        var buffer = "Hel\0o"u8;
        var reader = new SpanReader(buffer);

        var result = reader.ReadAscii();

        Assert.Equal("Hel", result);
        Assert.Equal(4, reader.Position);
    }

    [Fact]
    public void TestReadAsciiStringFixedLength()
    {
        var buffer = "Hello\0\0\0\0\0"u8;
        var reader = new SpanReader(buffer);

        var result = reader.ReadAscii(10);

        Assert.Equal("Hello", result);
        Assert.Equal(10, reader.Position);
    }

    [Fact]
    public void TestReadAsciiStringFixedLengthTooLarge()
    {
        Assert.Throws<EndOfStreamException>(
            () =>
            {
                var buffer = "Hel"u8;
                var reader = new SpanReader(buffer);
                reader.ReadAscii(10);
            }
        );
    }

    [Fact]
    public void TestReadAsciiSafe()
    {
        ReadOnlySpan<byte> buffer = [(byte)'H', (byte)'e', 0xFF, (byte)'l', (byte)'o'];
        var reader = new SpanReader(buffer);

        var result = reader.ReadAsciiSafe();

        Assert.Equal("He?lo", result);
        Assert.Equal(5, reader.Position);
    }

    [Fact]
    public void TestReadUTF8String()
    {
        var buffer = "Hello"u8;
        var reader = new SpanReader(buffer);

        var result = reader.ReadUTF8();

        Assert.Equal("Hello", result);
        Assert.Equal(5, reader.Position);
    }

    [Fact]
    public void TestReadUTF8StringWithNull()
    {
        var buffer = "Hi\0Bye"u8;
        var reader = new SpanReader(buffer);

        var result = reader.ReadUTF8();

        Assert.Equal("Hi", result);
        Assert.Equal(3, reader.Position);
    }

    [Fact]
    public void TestReadLittleUniString()
    {
        var buffer = "H\0i\0"u8;
        var reader = new SpanReader(buffer);

        var result = reader.ReadLittleUni();

        Assert.Equal("Hi", result);
        Assert.Equal(4, reader.Position);
    }

    [Fact]
    public void TestReadLittleUniStringWithNull()
    {
        var buffer = "H\0i\0\0\0X\0"u8;
        var reader = new SpanReader(buffer);

        var result = reader.ReadLittleUni();

        Assert.Equal("Hi", result);
        // Position 6: 4 bytes for "Hi" + 2 bytes for UTF-16 null terminator
        Assert.Equal(6, reader.Position);
    }

    [Fact]
    public void TestReadLittleUniStringFixedLength()
    {
        var buffer = "H\0i\0\0\0\0\0"u8;
        var reader = new SpanReader(buffer);

        var result = reader.ReadLittleUni(4);

        Assert.Equal("Hi", result);
        Assert.Equal(8, reader.Position);
    }

    [Fact]
    public void TestReadLittleUniSafe()
    {
        // Test with C1 control code (0x85 = NEL) which should be filtered
        ReadOnlySpan<byte> buffer = [(byte)'H', 0, 0x85, 0x00, (byte)'i', 0];
        var reader = new SpanReader(buffer);

        var result = reader.ReadLittleUniSafe();

        // C1 control (0x0085) removed - client renders nothing for invalid chars
        Assert.Equal("Hi", result);
        Assert.Equal(6, reader.Position);
    }

    [Fact]
    public void TestReadBigUniString()
    {
        var buffer = "\0H\0i"u8;
        var reader = new SpanReader(buffer);

        var result = reader.ReadBigUni();

        Assert.Equal("Hi", result);
        Assert.Equal(4, reader.Position);
    }

    [Fact]
    public void TestReadBigUniStringWithNull()
    {
        var buffer = "\0H\0i\0\0\0X"u8;
        var reader = new SpanReader(buffer);

        var result = reader.ReadBigUni();

        Assert.Equal("Hi", result);
        // Position 6: 4 bytes for "Hi" + 2 bytes for UTF-16 null terminator
        Assert.Equal(6, reader.Position);
    }

    [Fact]
    public void TestReadBigUniStringFixedLength()
    {
        var buffer = "\0H\0i\0\0\0\0"u8;
        var reader = new SpanReader(buffer);

        var result = reader.ReadBigUni(4);

        Assert.Equal("Hi", result);
        Assert.Equal(8, reader.Position);
    }

    [Fact]
    public void TestReadBigUniSafe()
    {
        // Test with C1 control code (0x0085 = NEL) which should be filtered
        ReadOnlySpan<byte> buffer = [0, (byte)'H', 0x00, 0x85, 0, (byte)'i'];
        var reader = new SpanReader(buffer);

        var result = reader.ReadBigUniSafe();

        // C1 control (0x0085) removed - client renders nothing for invalid chars
        Assert.Equal("Hi", result);
        Assert.Equal(6, reader.Position);
    }

    [Fact]
    public void TestSeekBegin()
    {
        ReadOnlySpan<byte> buffer = [0x01, 0x02, 0x03, 0x04, 0x05];
        var reader = new SpanReader(buffer);

        reader.ReadByte();
        reader.ReadByte();

        var pos = reader.Seek(0, SeekOrigin.Begin);

        Assert.Equal(0, pos);
        Assert.Equal(0, reader.Position);
        Assert.Equal(0x01, reader.ReadByte());
    }

    [Fact]
    public void TestSeekCurrent()
    {
        ReadOnlySpan<byte> buffer = [0x01, 0x02, 0x03, 0x04, 0x05];
        var reader = new SpanReader(buffer);

        reader.ReadByte();
        var pos = reader.Seek(2, SeekOrigin.Current);

        Assert.Equal(3, pos);
        Assert.Equal(3, reader.Position);
        Assert.Equal(0x04, reader.ReadByte());
    }

    [Fact]
    public void TestSeekEnd()
    {
        ReadOnlySpan<byte> buffer = [0x01, 0x02, 0x03, 0x04, 0x05];
        var reader = new SpanReader(buffer);

        var pos = reader.Seek(-2, SeekOrigin.End);

        Assert.Equal(3, pos);
        Assert.Equal(3, reader.Position);
        Assert.Equal(0x04, reader.ReadByte());
    }

    [Fact]
    public void TestSeekNegativeThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                ReadOnlySpan<byte> buffer = [0x01, 0x02, 0x03];
                var reader = new SpanReader(buffer);
                reader.Seek(-1, SeekOrigin.Begin);
            }
        );
    }

    [Fact]
    public void TestSeekBeyondEndThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                ReadOnlySpan<byte> buffer = [0x01, 0x02, 0x03];
                var reader = new SpanReader(buffer);
                reader.Seek(10, SeekOrigin.Begin);
            }
        );
    }

    [Fact]
    public void TestRead()
    {
        ReadOnlySpan<byte> buffer = [0x01, 0x02, 0x03, 0x04, 0x05];
        var reader = new SpanReader(buffer);

        Span<byte> dest = stackalloc byte[3];
        var bytesRead = reader.Read(dest);

        Assert.Equal(3, bytesRead);
        Assert.Equal(3, reader.Position);
        AssertThat.Equal(dest, [0x01, 0x02, 0x03]);
    }

    [Fact]
    public void TestReadPartial()
    {
        ReadOnlySpan<byte> buffer = [0x01, 0x02, 0x03];
        var reader = new SpanReader(buffer);

        Span<byte> dest = stackalloc byte[5];
        var bytesRead = reader.Read(dest);

        Assert.Equal(3, bytesRead);
        Assert.Equal(3, reader.Position);
        AssertThat.Equal(dest[..3], [0x01, 0x02, 0x03]);
    }

    [Fact]
    public void TestReadEmpty()
    {
        ReadOnlySpan<byte> buffer = [0x01, 0x02, 0x03];
        var reader = new SpanReader(buffer);

        Span<byte> dest = [];
        var bytesRead = reader.Read(dest);

        Assert.Equal(0, bytesRead);
        Assert.Equal(0, reader.Position);
    }

    [Fact]
    public void TestReadAtEnd()
    {
        ReadOnlySpan<byte> buffer = [0x01, 0x02, 0x03];
        var reader = new SpanReader(buffer);

        reader.Seek(3, SeekOrigin.Begin);

        Span<byte> dest = stackalloc byte[5];
        var bytesRead = reader.Read(dest);

        Assert.Equal(0, bytesRead);
        Assert.Equal(3, reader.Position);
    }

    [Fact]
    public void TestLength()
    {
        ReadOnlySpan<byte> buffer = [0x01, 0x02, 0x03, 0x04, 0x05];
        var reader = new SpanReader(buffer);

        Assert.Equal(5, reader.Length);
    }

    [Fact]
    public void TestPosition()
    {
        ReadOnlySpan<byte> buffer = [0x01, 0x02, 0x03];
        var reader = new SpanReader(buffer);

        Assert.Equal(0, reader.Position);

        reader.ReadByte();
        Assert.Equal(1, reader.Position);

        reader.ReadUInt16();
        Assert.Equal(3, reader.Position);
    }

    [Fact]
    public void TestRemaining()
    {
        ReadOnlySpan<byte> buffer = [0x01, 0x02, 0x03, 0x04, 0x05];
        var reader = new SpanReader(buffer);

        Assert.Equal(5, reader.Remaining);

        reader.ReadByte();
        Assert.Equal(4, reader.Remaining);

        reader.ReadUInt16();
        Assert.Equal(2, reader.Remaining);

        reader.ReadUInt16();
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void TestBuffer()
    {
        ReadOnlySpan<byte> buffer = [0x01, 0x02, 0x03];
        var reader = new SpanReader(buffer);

        var bufferProperty = reader.Buffer;

        Assert.Equal(3, bufferProperty.Length);
        AssertThat.Equal(bufferProperty, buffer);
    }

    [Fact]
    public void TestReadStringEmptyFixedLength()
    {
        ReadOnlySpan<byte> buffer = [0x01, 0x02, 0x03];
        var reader = new SpanReader(buffer);

        var result = reader.ReadAscii(0);

        Assert.Equal("", result);
        Assert.Equal(0, reader.Position);
    }

    [Fact]
    public void TestReadMultipleStringsWithNullTerminators()
    {
        var buffer = "AB\0CD\0EF"u8;
        var reader = new SpanReader(buffer);

        Assert.Equal("AB", reader.ReadAscii());
        Assert.Equal("CD", reader.ReadAscii());
        Assert.Equal("EF", reader.ReadAscii());
        Assert.Equal(8, reader.Position);
    }

    [Fact]
    public void TestReadLittleUniOddByteCount()
    {
        // If buffer has odd number of bytes, the last byte should be ignored
        ReadOnlySpan<byte> buffer = [(byte)'H', 0, (byte)'i', 0, 0xFF];
        var reader = new SpanReader(buffer);

        var result = reader.ReadLittleUni();

        Assert.Equal("Hi", result);
        Assert.Equal(4, reader.Position);
    }

    [Fact]
    public void TestReadBigUniOddByteCount()
    {
        // If buffer has odd number of bytes, the last byte should be ignored
        ReadOnlySpan<byte> buffer = [0, (byte)'H', 0, (byte)'i', 0xFF];
        var reader = new SpanReader(buffer);

        var result = reader.ReadBigUni();

        Assert.Equal("Hi", result);
        Assert.Equal(4, reader.Position);
    }
}
