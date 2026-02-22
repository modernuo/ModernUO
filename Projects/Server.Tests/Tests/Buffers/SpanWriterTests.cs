using System;
using System.Buffers;
using System.IO;
using Xunit;

namespace Server.Tests.Buffers;

public class SpanWriterTests
{
    [Fact]
    public unsafe void TestSpanWriterResizes()
    {
        Span<byte> smallStack = stackalloc byte[8];
        var writer = new SpanWriter(smallStack, true);
        writer.Write(0x1024L);
        writer.Write(0x1024L);

        var span = writer.RawBuffer;
        fixed (byte* spanPtr = span)
        {
            fixed (byte* stackPtr = smallStack)
            {
                Assert.True(spanPtr != stackPtr);
            }
        }

        Assert.True(span.Length > smallStack.Length);
        writer.Dispose();
    }

    [Fact]
    public unsafe void TestSpanWriterOnlyStackAlloc()
    {
        Span<byte> smallStack = stackalloc byte[8];
        var writer = new SpanWriter(smallStack, true);
        writer.Write(0x1024L);

        var span = writer.RawBuffer;
        fixed (byte* spanPtr = span)
        {
            fixed (byte* stackPtr = smallStack)
            {
                Assert.True(spanPtr == stackPtr);
            }
        }

        Assert.Equal(8, span.Length);
        AssertThat.Equal(smallStack, [0, 0, 0, 0, 0, 0, 0x10, 0x24]);
    }

    [Fact]
    public void TestSpanWriterNoResizeThrows()
    {
        Assert.Throws<InvalidOperationException>(
            () =>
            {
                Span<byte> smallStack = stackalloc byte[8];
                var writer = new SpanWriter(smallStack);
                writer.Write(0x1024L);
                writer.Write(0x1024L);
            }
        );
    }

    [Fact]
    public void TestWriteBool()
    {
        Span<byte> buffer = stackalloc byte[2];
        var writer = new SpanWriter(buffer);

        writer.Write(true);
        writer.Write(false);

        Assert.Equal(2, writer.Position);
        AssertThat.Equal(writer.Span, [1, 0]);
    }

    [Fact]
    public void TestWriteByte()
    {
        Span<byte> buffer = stackalloc byte[2];
        var writer = new SpanWriter(buffer);

        writer.Write((byte)0x12);
        writer.Write((byte)0x34);

        Assert.Equal(2, writer.Position);
        AssertThat.Equal(writer.Span, [0x12, 0x34]);
    }

    [Fact]
    public void TestWriteSByte()
    {
        Span<byte> buffer = stackalloc byte[2];
        var writer = new SpanWriter(buffer);

        writer.Write((sbyte)-1);
        writer.Write((sbyte)127);

        Assert.Equal(2, writer.Position);
        AssertThat.Equal(writer.Span, [0xFF, 0x7F]);
    }

    [Fact]
    public void TestWriteInt16BigEndian()
    {
        Span<byte> buffer = stackalloc byte[2];
        var writer = new SpanWriter(buffer);

        writer.Write((short)0x1234);

        Assert.Equal(2, writer.Position);
        AssertThat.Equal(writer.Span, [0x12, 0x34]);
    }

    [Fact]
    public void TestWriteInt16LittleEndian()
    {
        Span<byte> buffer = stackalloc byte[2];
        var writer = new SpanWriter(buffer);

        writer.WriteLE((short)0x1234);

        Assert.Equal(2, writer.Position);
        AssertThat.Equal(writer.Span, [0x34, 0x12]);
    }

    [Fact]
    public void TestWriteUInt16BigEndian()
    {
        Span<byte> buffer = stackalloc byte[2];
        var writer = new SpanWriter(buffer);

        writer.Write((ushort)0x1234);

        Assert.Equal(2, writer.Position);
        AssertThat.Equal(writer.Span, [0x12, 0x34]);
    }

    [Fact]
    public void TestWriteUInt16LittleEndian()
    {
        Span<byte> buffer = stackalloc byte[2];
        var writer = new SpanWriter(buffer);

        writer.WriteLE((ushort)0x1234);

        Assert.Equal(2, writer.Position);
        AssertThat.Equal(writer.Span, [0x34, 0x12]);
    }

    [Fact]
    public void TestWriteInt32BigEndian()
    {
        Span<byte> buffer = stackalloc byte[4];
        var writer = new SpanWriter(buffer);

        writer.Write(0x12345678);

        Assert.Equal(4, writer.Position);
        AssertThat.Equal(writer.Span, [0x12, 0x34, 0x56, 0x78]);
    }

    [Fact]
    public void TestWriteInt32LittleEndian()
    {
        Span<byte> buffer = stackalloc byte[4];
        var writer = new SpanWriter(buffer);

        writer.WriteLE(0x12345678);

        Assert.Equal(4, writer.Position);
        AssertThat.Equal(writer.Span, [0x78, 0x56, 0x34, 0x12]);
    }

    [Fact]
    public void TestWriteUInt32BigEndian()
    {
        Span<byte> buffer = stackalloc byte[4];
        var writer = new SpanWriter(buffer);

        writer.Write(0x12345678u);

        Assert.Equal(4, writer.Position);
        AssertThat.Equal(writer.Span, [0x12, 0x34, 0x56, 0x78]);
    }

    [Fact]
    public void TestWriteUInt32LittleEndian()
    {
        Span<byte> buffer = stackalloc byte[4];
        var writer = new SpanWriter(buffer);

        writer.WriteLE(0x12345678u);

        Assert.Equal(4, writer.Position);
        AssertThat.Equal(writer.Span, [0x78, 0x56, 0x34, 0x12]);
    }

    [Fact]
    public void TestWriteInt64BigEndian()
    {
        Span<byte> buffer = stackalloc byte[8];
        var writer = new SpanWriter(buffer);

        writer.Write(0x123456789ABCDEF0L);

        Assert.Equal(8, writer.Position);
        AssertThat.Equal(writer.Span, [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0]);
    }

    [Fact]
    public void TestWriteUInt64BigEndian()
    {
        Span<byte> buffer = stackalloc byte[8];
        var writer = new SpanWriter(buffer);

        writer.Write(0x123456789ABCDEF0UL);

        Assert.Equal(8, writer.Position);
        AssertThat.Equal(writer.Span, [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0]);
    }

    [Fact]
    public void TestWriteSpan()
    {
        Span<byte> buffer = stackalloc byte[6];
        var writer = new SpanWriter(buffer);

        ReadOnlySpan<byte> data = [0x01, 0x02, 0x03];
        writer.Write(data);
        writer.Write(data);

        Assert.Equal(6, writer.Position);
        AssertThat.Equal(writer.Span, [0x01, 0x02, 0x03, 0x01, 0x02, 0x03]);
    }

    [Fact]
    public void TestWriteAsciiChar()
    {
        Span<byte> buffer = stackalloc byte[2];
        var writer = new SpanWriter(buffer);

        writer.WriteAscii('A');
        writer.WriteAscii('B');

        Assert.Equal(2, writer.Position);
        AssertThat.Equal(writer.Span, "AB"u8);
    }

    [Fact]
    public void TestWriteAsciiString()
    {
        Span<byte> buffer = stackalloc byte[5];
        var writer = new SpanWriter(buffer);

        writer.WriteAscii("Hello");

        Assert.Equal(5, writer.Position);
        AssertThat.Equal(writer.Span, "Hello"u8);
    }

    [Fact]
    public void TestWriteAsciiStringNull()
    {
        Span<byte> buffer = stackalloc byte[6];
        var writer = new SpanWriter(buffer);

        writer.WriteAsciiNull("Hello");

        Assert.Equal(6, writer.Position);
        AssertThat.Equal(writer.Span, "Hello\0"u8);
    }

    [Fact]
    public void TestWriteAsciiStringFixedLength()
    {
        Span<byte> buffer = stackalloc byte[10];
        var writer = new SpanWriter(buffer);

        writer.WriteAscii("Hello", 10);

        Assert.Equal(10, writer.Position);
        AssertThat.Equal(writer.Span, "Hello\0\0\0\0\0"u8);
    }

    [Fact]
    public void TestWriteUTF8String()
    {
        Span<byte> buffer = stackalloc byte[5];
        var writer = new SpanWriter(buffer);

        writer.WriteUTF8("Hello");

        Assert.Equal(5, writer.Position);
        AssertThat.Equal(writer.Span, "Hello"u8);
    }

    [Fact]
    public void TestWriteUTF8StringNull()
    {
        Span<byte> buffer = stackalloc byte[6];
        var writer = new SpanWriter(buffer);

        writer.WriteUTF8Null("Hello");

        Assert.Equal(6, writer.Position);
        AssertThat.Equal(writer.Span, "Hello\0"u8);
    }

    [Fact]
    public void TestWriteLittleUniString()
    {
        Span<byte> buffer = stackalloc byte[10];
        var writer = new SpanWriter(buffer);

        writer.WriteLittleUni("Hello");

        Assert.Equal(10, writer.Position);
        AssertThat.Equal(writer.Span, "H\0e\0l\0l\0o\0"u8);
    }

    [Fact]
    public void TestWriteLittleUniStringNull()
    {
        Span<byte> buffer = stackalloc byte[12];
        var writer = new SpanWriter(buffer);

        writer.WriteLittleUniNull("Hello");

        Assert.Equal(12, writer.Position);
        AssertThat.Equal(writer.Span, "H\0e\0l\0l\0o\0\0\0"u8);
    }

    [Fact]
    public void TestWriteBigUniString()
    {
        Span<byte> buffer = stackalloc byte[10];
        var writer = new SpanWriter(buffer);

        writer.WriteBigUni("Hello");

        Assert.Equal(10, writer.Position);
        AssertThat.Equal(writer.Span, "\0H\0e\0l\0l\0o"u8);
    }

    [Fact]
    public void TestWriteBigUniStringNull()
    {
        Span<byte> buffer = stackalloc byte[12];
        var writer = new SpanWriter(buffer);

        writer.WriteBigUniNull("Hello");

        Assert.Equal(12, writer.Position);
        AssertThat.Equal(writer.Span, "\0H\0e\0l\0l\0o\0\0"u8);
    }

    [Fact]
    public void TestClear()
    {
        Span<byte> buffer = stackalloc byte[10];
        buffer.Fill(0xFF);
        var writer = new SpanWriter(buffer);

        writer.Write((byte)0x01);
        writer.Clear(5);
        writer.Write((byte)0x02);

        Assert.Equal(7, writer.Position);
        AssertThat.Equal(writer.Span, [0x01, 0, 0, 0, 0, 0, 0x02]);
    }

    [Fact]
    public void TestSeekBegin()
    {
        Span<byte> buffer = stackalloc byte[10];
        var writer = new SpanWriter(buffer);

        writer.Write((byte)0x01);
        writer.Write((byte)0x02);
        writer.Write((byte)0x03);

        var pos = writer.Seek(1, SeekOrigin.Begin);

        Assert.Equal(1, pos);
        Assert.Equal(1, writer.Position);
        Assert.Equal(3, writer.BytesWritten);
    }

    [Fact]
    public void TestSeekCurrent()
    {
        Span<byte> buffer = stackalloc byte[10];
        var writer = new SpanWriter(buffer);

        writer.Write((byte)0x01);
        writer.Write((byte)0x02);

        var pos = writer.Seek(2, SeekOrigin.Current);

        Assert.Equal(4, pos);
        Assert.Equal(4, writer.Position);
    }

    [Fact]
    public void TestSeekEnd()
    {
        Span<byte> buffer = stackalloc byte[10];
        var writer = new SpanWriter(buffer);

        writer.Write((byte)0x01);
        writer.Write((byte)0x02);
        writer.Write((byte)0x03);

        var pos = writer.Seek(-1, SeekOrigin.End);

        Assert.Equal(2, pos);
        Assert.Equal(2, writer.Position);
    }

    [Fact]
    public void TestSeekNegativeThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                Span<byte> buffer = stackalloc byte[10];
                var writer = new SpanWriter(buffer);
                writer.Seek(-1, SeekOrigin.Begin);
            }
        );
    }

    [Fact]
    public void TestSeekBeyondCapacityNoResizeThrows()
    {
        Assert.Throws<InvalidOperationException>(
            () =>
            {
                Span<byte> buffer = stackalloc byte[10];
                var writer = new SpanWriter(buffer);
                writer.Seek(20, SeekOrigin.Begin);
            }
        );
    }

    [Fact]
    public void TestSeekBeyondCapacityWithResize()
    {
        var writer = new SpanWriter(10, true);

        var pos = writer.Seek(20, SeekOrigin.Begin);

        Assert.Equal(20, pos);
        Assert.True(writer.Capacity >= 20);
        writer.Dispose();
    }

    [Fact]
    public void TestEnsureCapacityNoResize()
    {
        Span<byte> buffer = stackalloc byte[10];
        var writer = new SpanWriter(buffer);

        writer.EnsureCapacity(10);
        Assert.Equal(10, writer.Capacity);
    }

    [Fact]
    public void TestEnsureCapacityNoResizeThrows()
    {
        Assert.Throws<InvalidOperationException>(
            () =>
            {
                Span<byte> buffer = stackalloc byte[10];
                var writer = new SpanWriter(buffer);
                writer.EnsureCapacity(20);
            }
        );
    }

    [Fact]
    public void TestEnsureCapacityWithResize()
    {
        var writer = new SpanWriter(10, true);

        writer.EnsureCapacity(50);

        Assert.True(writer.Capacity >= 50);
        writer.Dispose();
    }

    [Fact]
    public void TestToSpanWithStackAlloc()
    {
        Span<byte> buffer = stackalloc byte[10];
        var writer = new SpanWriter(buffer);

        writer.Write((byte)0x01);
        writer.Write((byte)0x02);
        writer.Write((byte)0x03);

        using var owner = writer.ToSpan();

        Assert.Equal(3, owner.Span.Length);
        AssertThat.Equal(owner.Span, [0x01, 0x02, 0x03]);
    }

    [Fact]
    public void TestToSpanWithRentedBuffer()
    {
        var writer = new SpanWriter(10);

        writer.Write((byte)0x01);
        writer.Write((byte)0x02);
        writer.Write((byte)0x03);

        using var owner = writer.ToSpan();

        Assert.Equal(3, owner.Span.Length);
        AssertThat.Equal(owner.Span, [0x01, 0x02, 0x03]);
    }

    [Fact]
    public void TestToSpanEmpty()
    {
        var writer = new SpanWriter(10);

        using var owner = writer.ToSpan();

        Assert.Equal(0, owner.Span.Length);
    }

    [Fact]
    public void TestBytesWritten()
    {
        Span<byte> buffer = stackalloc byte[10];
        var writer = new SpanWriter(buffer);

        Assert.Equal(0, writer.BytesWritten);

        writer.Write((byte)0x01);
        Assert.Equal(1, writer.BytesWritten);

        writer.Write((ushort)0x0203);
        Assert.Equal(3, writer.BytesWritten);

        writer.Seek(1, SeekOrigin.Begin);
        Assert.Equal(3, writer.BytesWritten);

        writer.Write((byte)0xFF);
        Assert.Equal(3, writer.BytesWritten);
    }

    [Fact]
    public void TestCapacity()
    {
        Span<byte> buffer = stackalloc byte[10];
        var writer = new SpanWriter(buffer);

        Assert.Equal(10, writer.Capacity);
    }

    [Fact]
    public void TestRawBuffer()
    {
        Span<byte> buffer = stackalloc byte[10];
        var writer = new SpanWriter(buffer);

        writer.Write((byte)0x01);

        var raw = writer.RawBuffer;
        Assert.Equal(10, raw.Length);
        Assert.Equal(0x01, raw[0]);
    }

    [Fact]
    public void TestSpan()
    {
        Span<byte> buffer = stackalloc byte[10];
        var writer = new SpanWriter(buffer);

        writer.Write((byte)0x01);
        writer.Write((byte)0x02);

        var span = writer.Span;
        Assert.Equal(2, span.Length);
        AssertThat.Equal(span, [0x01, 0x02]);
    }
}
