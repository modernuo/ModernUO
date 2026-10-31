using System;
using System.Buffers.Binary;
using System.Text;
using Xunit;

namespace Server.Tests.Network;

public class UOClientTests
{
    private const uint ImageBase = 0x400000;
    private const uint SectionVa = 0x1000;
    private const int SectionOffset = 0x200;
    private const int FormatOffset = SectionOffset + 0x20;
    private const int LetterOffset = SectionOffset + 0x30;

    // A minimal PE32 image with one section holding the pre-resource client's sprintf call:
    // push letter, push revision, push minor, push major, push "%d.%d.%d%s"
    private static byte[] LegacyClient(byte major, byte minor, byte revision, string letter, int calls = 1)
    {
        var exe = new byte[0x400];
        BinaryPrimitives.WriteInt32LittleEndian(exe.AsSpan(0x3C), 0x40);
        BinaryPrimitives.WriteUInt32LittleEndian(exe.AsSpan(0x40), 0x00004550); // PE\0\0
        BinaryPrimitives.WriteUInt16LittleEndian(exe.AsSpan(0x44), 0x14C);      // i386
        BinaryPrimitives.WriteUInt16LittleEndian(exe.AsSpan(0x46), 1);          // section count
        BinaryPrimitives.WriteUInt16LittleEndian(exe.AsSpan(0x54), 0xE0);       // optional header size
        BinaryPrimitives.WriteUInt16LittleEndian(exe.AsSpan(0x58), 0x10B);      // PE32
        BinaryPrimitives.WriteUInt32LittleEndian(exe.AsSpan(0x58 + 28), ImageBase);

        var section = exe.AsSpan(0x58 + 0xE0);
        BinaryPrimitives.WriteUInt32LittleEndian(section[12..], SectionVa);
        BinaryPrimitives.WriteUInt32LittleEndian(section[16..], 0x200); // raw size
        BinaryPrimitives.WriteUInt32LittleEndian(section[20..], SectionOffset);

        for (var i = 0; i < calls; i++)
        {
            var code = exe.AsSpan(SectionOffset + i * 16);
            code[0] = 0x68;
            BinaryPrimitives.WriteUInt32LittleEndian(code[1..], ImageBase + SectionVa + (LetterOffset - SectionOffset));
            code[5] = 0x6A;
            code[6] = revision;
            code[7] = 0x6A;
            code[8] = minor;
            code[9] = 0x6A;
            code[10] = major;
            code[11] = 0x68;
            BinaryPrimitives.WriteUInt32LittleEndian(code[12..], ImageBase + SectionVa + (FormatOffset - SectionOffset));
        }

        Encoding.ASCII.GetBytes("%d.%d.%d%s\0").CopyTo(exe, FormatOffset);
        Encoding.ASCII.GetBytes(letter + "\0").CopyTo(exe, LetterOffset);
        return exe;
    }

    [Theory]
    [InlineData(4, 0, 11, "c", "4.0.11c", 3)]
    [InlineData(5, 0, 1, "", "5.0.1", 0)]
    public void DetectsPreResourceClientFromVersionFormat(
        byte major, byte minor, byte revision, string letter, string expected, int patch
    )
    {
        var version = UOClient.DetectClassicClient(LegacyClient(major, minor, revision, letter));

        Assert.NotNull(version);
        Assert.Equal(expected, version.ToString());
        Assert.Equal(new ClientVersion(major, minor, revision, patch), version);
    }

    [Fact]
    public void IgnoresAmbiguousVersionFormatCalls()
    {
        Assert.Null(UOClient.DetectClassicClient(LegacyClient(4, 0, 11, "c", calls: 2)));
    }

    [Fact]
    public void IgnoresVersionFormatWithUnexpectedLetter()
    {
        Assert.Null(UOClient.DetectClassicClient(LegacyClient(4, 0, 11, "cd")));
    }

    [Fact]
    public void DetectsClientFromVersionInfo()
    {
        var exe = new byte[0x100];
        Encoding.Unicode.GetBytes("VS_VERSION_INFO").CopyTo(exe, 0x10);
        var offset = 0x10 + 42;
        BinaryPrimitives.WriteUInt16LittleEndian(exe.AsSpan(offset), 0);       // minor
        BinaryPrimitives.WriteUInt16LittleEndian(exe.AsSpan(offset + 2), 7);   // major
        BinaryPrimitives.WriteUInt16LittleEndian(exe.AsSpan(offset + 4), 0);   // private
        BinaryPrimitives.WriteUInt16LittleEndian(exe.AsSpan(offset + 6), 103); // build

        var version = UOClient.DetectClassicClient(exe);

        Assert.Equal(new ClientVersion(7, 0, 103, 0), version);
    }

    [Fact]
    public void ReturnsNullWithoutVersion()
    {
        Assert.Null(UOClient.DetectClassicClient(new byte[0x400]));
    }
}
