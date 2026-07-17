using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Server.Tests;

/// <summary>
/// Pins BufferWriter's byte-level output and position semantics so the write path can be
/// optimized without behavioral drift.
/// </summary>
public class BufferWriterTests
{
    [Fact]
    public void PrimitivesAreLittleEndianAtExpectedOffsets()
    {
        var writer = new BufferWriter(new byte[256], true);

        writer.Write((byte)0xAB);
        writer.Write((sbyte)-5);
        writer.Write(true);
        writer.Write(false);
        writer.Write((short)-12345);
        writer.Write((ushort)54321);
        writer.Write(-123456789);
        writer.Write(3123456789u);
        writer.Write(-1234567890123456789L);
        writer.Write(12345678901234567890UL);
        writer.Write(1234.5678d);
        writer.Write(56.75f);
        writer.Write((Serial)0x40000001u);

        Assert.Equal(1 + 1 + 1 + 1 + 2 + 2 + 4 + 4 + 8 + 8 + 8 + 4 + 4, writer.Position);

        var b = writer.Buffer;
        Assert.Equal(0xAB, b[0]);
        Assert.Equal(unchecked((byte)-5), b[1]);
        Assert.Equal(1, b[2]);
        Assert.Equal(0, b[3]);
        Assert.Equal(-12345, BinaryPrimitives.ReadInt16LittleEndian(b.AsSpan(4)));
        Assert.Equal(54321, BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(6)));
        Assert.Equal(-123456789, BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(8)));
        Assert.Equal(3123456789u, BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(12)));
        Assert.Equal(-1234567890123456789L, BinaryPrimitives.ReadInt64LittleEndian(b.AsSpan(16)));
        Assert.Equal(12345678901234567890UL, BinaryPrimitives.ReadUInt64LittleEndian(b.AsSpan(24)));
        Assert.Equal(1234.5678d, BinaryPrimitives.ReadDoubleLittleEndian(b.AsSpan(32)));
        Assert.Equal(56.75f, BinaryPrimitives.ReadSingleLittleEndian(b.AsSpan(40)));
        Assert.Equal(0x40000001u, BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(44)));
    }

    [Fact]
    public void SeekEndUsesHighWaterMarkNotCurrentPosition()
    {
        var writer = new BufferWriter(new byte[256], true);

        writer.Write(1L);
        writer.Write(2L);
        writer.Write(3L); // high water = 24

        writer.Seek(4, SeekOrigin.Begin);
        writer.Write(99); // position now 8, high water still 24

        Assert.Equal(24, writer.Seek(0, SeekOrigin.End));
        Assert.Equal(24, writer.Position);
    }

    [Fact]
    public void SeekCurrentAndBeginBehave()
    {
        var writer = new BufferWriter(new byte[64], true);

        writer.Write(0xDEADBEEF);
        Assert.Equal(2, writer.Seek(2, SeekOrigin.Begin));
        Assert.Equal(3, writer.Seek(1, SeekOrigin.Current));

        writer.Write((byte)0x77);
        Assert.Equal(0x77, writer.Buffer[3]);
    }

    [Fact]
    public void GrowthPreservesContentAndPosition()
    {
        var writer = new BufferWriter(new byte[16], true);

        for (var i = 0; i < 100; i++)
        {
            writer.Write((long)i);
        }

        Assert.Equal(800, writer.Position);
        Assert.True(writer.Buffer.Length >= 800);

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(i, BinaryPrimitives.ReadInt64LittleEndian(writer.Buffer.AsSpan(i * 8)));
        }
    }

    [Fact]
    public void SpanWriteCrossesGrowthBoundary()
    {
        var writer = new BufferWriter(new byte[8], true);

        Span<byte> payload = stackalloc byte[64];
        for (var i = 0; i < payload.Length; i++)
        {
            payload[i] = (byte)(i + 1);
        }

        writer.Write((ushort)7);
        writer.Write(payload);

        Assert.Equal(66, writer.Position);
        Assert.Equal(payload.ToArray(), writer.Buffer.AsSpan(2, 64).ToArray());
    }

    [Theory]
    [InlineData(0, new byte[] { 0x00 })]
    [InlineData(127, new byte[] { 0x7F })]
    [InlineData(128, new byte[] { 0x80, 0x01 })]
    [InlineData(0x3FFF, new byte[] { 0xFF, 0x7F })]
    [InlineData(0x4000, new byte[] { 0x80, 0x80, 0x01 })]
    [InlineData(0x1F_FFFF, new byte[] { 0xFF, 0xFF, 0x7F })]
    [InlineData(0x20_0000, new byte[] { 0x80, 0x80, 0x80, 0x01 })]
    [InlineData(0xFFF_FFFF, new byte[] { 0xFF, 0xFF, 0xFF, 0x7F })]
    [InlineData(0x1000_0000, new byte[] { 0x80, 0x80, 0x80, 0x80, 0x01 })]
    [InlineData(int.MaxValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x07 })]
    [InlineData(-1, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x0F })]
    public void EncodedIntMatchesFormat(int value, byte[] expected)
    {
        var writer = new BufferWriter(new byte[16], true);

        ((IGenericWriter)writer).WriteEncodedInt(value);

        Assert.Equal(expected.Length, writer.Position);
        Assert.Equal(expected, writer.Buffer.AsSpan(0, expected.Length).ToArray());
    }

    [Fact]
    public void PrefixedStringsWriteFlagLengthAndUtf8()
    {
        var writer = new BufferWriter(new byte[256], true);

        writer.Write("héllo Ωorld");
        var utf8 = Encoding.UTF8.GetBytes("héllo Ωorld");

        var b = writer.Buffer;
        Assert.Equal(1, b[0]); // not-null flag
        Assert.Equal(utf8.Length, b[1]); // encoded length (small string = 1 byte)
        Assert.Equal(utf8, b.AsSpan(2, utf8.Length).ToArray());
        Assert.Equal(2 + utf8.Length, writer.Position);

        writer.Write((string)null);
        Assert.Equal(0, b[2 + utf8.Length]); // null flag
    }

    [Theory]
    [InlineData(84)]  // scratch path
    [InlineData(85)]  // scratch path boundary
    [InlineData(86)]  // two-pass path
    [InlineData(300)] // two-pass, length prefix > 1 byte
    public void StringPathsAgreeAcrossTheScratchBoundary(int chars)
    {
        // Mixed ASCII, 2-byte, 3-byte, and surrogate-pair (4-byte) content
        var builder = new StringBuilder(chars);
        for (var i = 0; builder.Length < chars; i++)
        {
            switch (i % 4)
            {
                case 0:
                    builder.Append('a');
                    break;
                case 1:
                    builder.Append('é');
                    break;
                case 2:
                    builder.Append('Ω');
                    break;
                default:
                    if (builder.Length + 2 <= chars)
                    {
                        builder.Append("𝔘"); // surrogate pair
                    }
                    else
                    {
                        builder.Append('z');
                    }
                    break;
            }
        }

        var value = builder.ToString();
        Assert.Equal(chars, value.Length);

        var writer = new BufferWriter(new byte[16], true); // forces growth through both paths
        writer.Write(value);

        var utf8 = Encoding.UTF8.GetBytes(value);
        var b = writer.Buffer;
        Assert.Equal(1, b[0]);

        // decode the 7-bit encoded length prefix
        var offset = 1;
        var length = 0;
        var shift = 0;
        byte current;
        do
        {
            current = b[offset++];
            length |= (current & 0x7F) << shift;
            shift += 7;
        } while ((current & 0x80) != 0);

        Assert.Equal(utf8.Length, length);
        Assert.Equal(utf8, b.AsSpan(offset, length).ToArray());
        Assert.Equal(offset + length, writer.Position);
    }

    [Fact]
    public void DateTimeWritesUtcTicksViaInterface()
    {
        var writer = new BufferWriter(new byte[64], true);
        IGenericWriter iface = writer;

        var utc = new DateTime(2026, 7, 13, 1, 2, 3, DateTimeKind.Utc);
        var local = utc.ToLocalTime();

        iface.Write(utc);
        iface.Write(local); // must convert to UTC

        Assert.Equal(utc.Ticks, BinaryPrimitives.ReadInt64LittleEndian(writer.Buffer.AsSpan(0)));
        Assert.Equal(utc.Ticks, BinaryPrimitives.ReadInt64LittleEndian(writer.Buffer.AsSpan(8)));
    }

    [Fact]
    public void Point3DWritesThreeInts()
    {
        var writer = new BufferWriter(new byte[64], true);
        IGenericWriter iface = writer;

        iface.Write(new Point3D(100, -200, 30));

        Assert.Equal(12, writer.Position);
        Assert.Equal(100, BinaryPrimitives.ReadInt32LittleEndian(writer.Buffer.AsSpan(0)));
        Assert.Equal(-200, BinaryPrimitives.ReadInt32LittleEndian(writer.Buffer.AsSpan(4)));
        Assert.Equal(30, BinaryPrimitives.ReadInt32LittleEndian(writer.Buffer.AsSpan(8)));
    }

    [Fact]
    public void DecimalRoundTripsThroughReader()
    {
        var writer = new BufferWriter(new byte[64], true);
        writer.Write(1234567.89012m);

        IGenericReader reader = new BufferReader(writer.Buffer);
        Assert.Equal(1234567.89012m, reader.ReadDecimal());
    }

    [Fact]
    public void LongStringPrefixIsZeroPaddedToWorstCaseWidth()
    {
        // 100 ASCII chars: worst case 300 bytes -> 2-byte prefix; actual 100 bytes would
        // canonically fit in 1. The prefix must be the non-minimal 2-byte form the readers
        // decode identically: (100 | 0x80), 0x00.
        var value = new string('a', 100);
        var writer = new BufferWriter(new byte[1024], false);
        writer.WriteRaw(value);

        var b = writer.Buffer;
        Assert.Equal((byte)(100 | 0x80), b[0]);
        Assert.Equal(0, b[1]);
        Assert.Equal(2 + 100, writer.Position);

        IGenericReader reader = new BufferReader(writer.Buffer);
        Assert.Equal(value, reader.ReadStringRaw());
    }

    [Theory]
    [InlineData(42)]     // canonical 1-byte prefix (3 * 42 < 0x80)
    [InlineData(100)]    // padded prefix
    [InlineData(10_000)] // multi-byte prefix, forces growth from a small buffer
    public void ThreeBytePerCharContentRoundTrips(int chars)
    {
        // CJK content encodes at the UTF-8 worst case of 3 bytes per char - the case an
        // undersized scratch would truncate or throw on.
        var value = new string('二', chars);
        var writer = new BufferWriter(new byte[16], true);
        writer.Write(value);

        Assert.Equal(Encoding.UTF8.GetByteCount(value), 3 * chars);

        IGenericReader reader = new BufferReader(writer.Buffer);
        Assert.Equal(value, reader.ReadString());
    }

    [Fact]
    public void SurrogatePairContentRoundTripsThroughRawPath()
    {
        // Surrogate pairs encode 2 chars into 4 bytes (2 bytes per char) - under the
        // 3-bytes-per-char reservation, exercising written < maxLength with a padded prefix.
        var value = string.Concat(Enumerable.Repeat("😀", 60)); // 120 chars, 240 bytes
        var writer = new BufferWriter(new byte[16], false);
        writer.WriteRaw(value);

        IGenericReader reader = new BufferReader(writer.Buffer);
        Assert.Equal(value, reader.ReadStringRaw());
    }
}
