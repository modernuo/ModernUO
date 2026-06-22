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
}
