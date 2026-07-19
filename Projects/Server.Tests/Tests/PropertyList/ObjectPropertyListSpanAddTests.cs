using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using Server;
using Xunit;

namespace Server.Tests;

public class ObjectPropertyListSpanAddTests
{
    // Decodes a terminated OPL buffer into (cliloc, argument) entries.
    // The OPL packet is big-endian (SpanWriter default), so use BinaryPrimitives.
    private static (int cliloc, string arg)[] Decode(ObjectPropertyList opl)
    {
        opl.Terminate();
        var buffer = opl.Buffer;
        var entries = new List<(int, string)>();
        var pos = 15; // header is 15 bytes
        while (true)
        {
            var cliloc = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(pos));
            pos += 4;
            if (cliloc == 0)
            {
                break;
            }

            var byteLen = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(pos));
            pos += 2;
            var arg = Encoding.Unicode.GetString(buffer, pos, byteLen);
            pos += byteLen;
            entries.Add((cliloc, arg));
        }

        return entries.ToArray();
    }

    [Fact]
    public void SpanAdd_ProducesSameBytesAsStringArgument()
    {
        // Add(int, string) routes through the interpolation InternalAdd; Add(int, ReadOnlySpan<char>)
        // through the span InternalAdd. Both must produce identical bytes and hash.
        var fromString = new ObjectPropertyList(null);
        fromString.Add(1070722, "Hello World");

        var fromSpan = new ObjectPropertyList(null);
        fromSpan.Add(1070722, "Hello World".AsSpan());

        Assert.Equal(Decode(fromString), Decode(fromSpan));
        Assert.Equal(fromString.Hash, fromSpan.Hash);
    }

    [Fact]
    public void SpanAdd_WithNumber_EmitsClilocAndArgument()
    {
        var opl = new ObjectPropertyList(null);
        opl.Add(1070722, "Custom".AsSpan());

        var entries = Decode(opl);
        Assert.Single(entries);
        Assert.Equal((1070722, "Custom"), entries[0]);
    }

    [Fact]
    public void HashFormat_OnlyMarksIntegers()
    {
        // Integer {value:#} emits the cliloc marker "#<value>".
        var intList = new ObjectPropertyList(null);
        intList.Add(1062028, $"{1043009:#}");
        Assert.Equal((1062028, "#1043009"), Decode(intList)[0]);

        // Float {value:#} is the standard '#' custom-numeric (digit-placeholder) format, not a cliloc
        // marker -- so no leading '#'.
        var dblList = new ObjectPropertyList(null);
        dblList.Add(1062028, $"{42.0:#}");
        Assert.Equal((1062028, 42.0.ToString("#")), Decode(dblList)[0]); // "42"
    }

    [Fact]
    public void Add_TruncatesArgumentOverMaxLength()
    {
        var oversized = new string('x', ObjectPropertyList.MaxArgumentLength + 50);

        var opl = new ObjectPropertyList(null);
        opl.Add(1070722, oversized.AsSpan());

        var entries = Decode(opl);
        Assert.Single(entries);
        Assert.Equal(ObjectPropertyList.MaxArgumentLength, entries[0].arg.Length);
    }

    [Fact]
    public void AddChunked_SplitsAtNewlinesSoNoEntryExceedsCap()
    {
        // 10 lines x 100 chars joined by '\n' (~1009 chars), well over the cap.
        var lines = new string[10];
        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = new string((char)('a' + i), 100);
        }

        var text = string.Join("\n", lines);

        var opl = new ObjectPropertyList(null);
        opl.AddChunked(text.AsSpan());

        var entries = Decode(opl);
        Assert.True(entries.Length > 1, "expected multiple chunks");
        foreach (var (_, arg) in entries)
        {
            Assert.True(arg.Length <= ObjectPropertyList.MaxArgumentLength);
        }

        // AddChunked breaks only at '\n' (dropping that '\n'), so rejoining with '\n' is lossless.
        var rejoined = string.Join("\n", Array.ConvertAll(entries, e => e.arg));
        Assert.Equal(text, rejoined);
    }
}
