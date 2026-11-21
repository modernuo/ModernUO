using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Server.Tests;

public class GenericReaderTests
{
    private enum ByteEnum : byte { Value = 0xAB }

    private enum ShortEnum : short { Value = -12345 }

    private enum IntEnum { Value = 0x0ABCDEF }

    private enum LongEnum : long { Value = long.MinValue + 7 }

    public static TheoryData<string> Readers => ["buffer", "unmanaged", "defaults"];

    // The pinned array is returned so the caller keeps it alive while the unmanaged reader points into it.
    private static unsafe (IGenericReader Reader, byte[] Data) CreateReader(string kind, ReadOnlySpan<byte> bytes)
    {
        var data = GC.AllocateArray<byte>(bytes.Length, pinned: true);
        bytes.CopyTo(data);

        IGenericReader reader = kind switch
        {
            "buffer"    => new BufferReader(data),
            "defaults"  => new DefaultMembersReader(new BufferReader(data)),
            _           => new UnmanagedDataReader(data.Length == 0 ? null : (byte*)Unsafe.AsPointer(ref data[0]), data.Length)
        };

        return (reader, data);
    }

    /// <summary>
    /// Implements only the members IGenericReader requires, so every other read uses the
    /// interface's default implementation, as a reader outside the core would.
    /// </summary>
    private sealed class DefaultMembersReader(BufferReader inner) : IGenericReader
    {
        public string ReadString(bool intern = false) => inner.ReadString(intern);
        public string ReadStringRaw(bool intern = false) => inner.ReadStringRaw(intern);
        public long ReadLong() => inner.ReadLong();
        public ulong ReadULong() => inner.ReadULong();
        public int ReadInt() => inner.ReadInt();
        public uint ReadUInt() => inner.ReadUInt();
        public short ReadShort() => inner.ReadShort();
        public ushort ReadUShort() => inner.ReadUShort();
        public double ReadDouble() => inner.ReadDouble();
        public float ReadFloat() => inner.ReadFloat();
        public byte ReadByte() => inner.ReadByte();
        public sbyte ReadSByte() => inner.ReadSByte();
        public bool ReadBool() => inner.ReadBool();
        public Serial ReadSerial() => inner.ReadSerial();
        public Type ReadType() => inner.ReadType();
        public int Read(Span<byte> buffer) => inner.Read(buffer);
        public long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
    }

    [Theory]
    [MemberData(nameof(Readers))]
    public void ReadsBackWhatBufferWriterWrote(string kind)
    {
        int[] encoded = [0, 0x7F, 0x80, 0x3FFF, 0x4000, 0x1FFFFF, 0x200000, 0xFFFFFFF, 0x10000000, int.MaxValue, -1];
        var created = new DateTime(2026, 11, 21, 12, 34, 56, DateTimeKind.Utc);

        var writer = new BufferWriter(16, true);
        foreach (var value in encoded)
        {
            writer.WriteEncodedInt(value);
        }

        writer.Write(new Point3D(-5, 70000, -128));
        writer.Write(new Point2D(int.MinValue, int.MaxValue));
        writer.Write(new Rectangle3D(new Point3D(1, 2, 3), new Point3D(4, 5, 6)));
        writer.Write(created);
        writer.Write(TimeSpan.FromMinutes(90));
        writer.WriteEnum(ByteEnum.Value);
        writer.WriteEnum(ShortEnum.Value);
        writer.WriteEnum(IntEnum.Value);
        writer.WriteEnum(LongEnum.Value);
        writer.Write((string)null);
        writer.Write("");
        writer.Write("héllo 二");
        writer.Write((Type)null);
        writer.Write(0x01020304);

        var (reader, data) = CreateReader(kind, writer.Buffer.AsSpan(0, (int)writer.Position));

        foreach (var value in encoded)
        {
            Assert.Equal(value, reader.ReadEncodedInt());
        }

        Assert.Equal(new Point3D(-5, 70000, -128), reader.ReadPoint3D());
        Assert.Equal(new Point2D(int.MinValue, int.MaxValue), reader.ReadPoint2D());
        Assert.Equal(new Rectangle3D(new Point3D(1, 2, 3), new Point3D(4, 5, 6)), reader.ReadRect3D());
        Assert.Equal(created, reader.ReadDateTime());
        Assert.Equal(TimeSpan.FromMinutes(90), reader.ReadTimeSpan());
        Assert.Equal(ByteEnum.Value, reader.ReadEnum<ByteEnum>());
        Assert.Equal(ShortEnum.Value, reader.ReadEnum<ShortEnum>());
        Assert.Equal(IntEnum.Value, reader.ReadEnum<IntEnum>());
        Assert.Equal(LongEnum.Value, reader.ReadEnum<LongEnum>());
        Assert.Null(reader.ReadString());
        Assert.Equal("", reader.ReadString());
        Assert.Equal("héllo 二", reader.ReadString());
        Assert.Null(reader.ReadType());
        Assert.Equal(0x01020304, reader.ReadInt());
        Assert.Equal(data.Length, reader.Position);
    }

    [Theory]
    [MemberData(nameof(Readers))]
    public void StringLengthPastTheEndThrowsInsteadOfReadingBeyondTheData(string kind)
    {
        // Prefix flag, then a 2-byte encoded length of 1000 with only three bytes after it.
        var (reader, _) = CreateReader(kind, [1, 0xE8, 0x07, (byte)'a', (byte)'b', (byte)'c']);

        Assert.Throws<EndOfStreamException>(() => reader.ReadString());
    }

    [Theory]
    [MemberData(nameof(Readers))]
    public void PrimitivePastTheEndThrows(string kind)
    {
        var (reader, _) = CreateReader(kind, [1, 2, 3]);

        Assert.Throws<EndOfStreamException>(() => reader.ReadInt());
    }

    [Theory]
    [MemberData(nameof(Readers))]
    public void EncodedIntLongerThanFiveBytesThrows(string kind)
    {
        var (reader, _) = CreateReader(kind, [0x80, 0x80, 0x80, 0x80, 0x80, 0x01]);

        Assert.Throws<InvalidDataException>(() => reader.ReadEncodedInt());
    }

    [Theory]
    [MemberData(nameof(Readers))]
    public void SeekAllowsTheEndAndRejectsOutsideTheData(string kind)
    {
        var (reader, _) = CreateReader(kind, [1, 2, 3, 4]);

        Assert.Equal(4, reader.Seek(0, SeekOrigin.End));
        Assert.Equal(2, reader.Seek(-2, SeekOrigin.Current));
        Assert.Equal(3, reader.ReadByte());

        Assert.Throws<ArgumentOutOfRangeException>(() => reader.Seek(5, SeekOrigin.Begin));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.Seek(-4, SeekOrigin.Current));
    }
}
