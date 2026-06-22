using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using Server;
using Xunit;

namespace Server.Tests;

public class OplTextBlockTests
{
    private static (int cliloc, string arg)[] Decode(ObjectPropertyList opl)
    {
        opl.Terminate();
        var buffer = opl.Buffer;
        var entries = new List<(int, string)>();
        var pos = 15;
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
            entries.Add((cliloc, Encoding.Unicode.GetString(buffer, pos, byteLen)));
            pos += byteLen;
        }

        return entries.ToArray();
    }

    [Fact]
    public void MultipleLines_JoinIntoSingleCyclingEntry()
    {
        var opl = new ObjectPropertyList(null);
        using (var block = opl.TextBlock())
        {
            block.Add("Line One".AsSpan());
            block.Add("Line Two".AsSpan());
        }

        var entries = Decode(opl);
        Assert.Single(entries);
        Assert.Equal((1042971, "Line One\nLine Two"), entries[0]); // first cycling cliloc
    }

    [Fact]
    public void EmptyLines_AreSkipped()
    {
        var opl = new ObjectPropertyList(null);
        using (var block = opl.TextBlock())
        {
            block.Add("Only".AsSpan());
            block.Add(ReadOnlySpan<char>.Empty);
        }

        Assert.Equal((1042971, "Only"), Decode(opl)[0]);
    }

    [Fact]
    public void NoLines_EmitsNothing()
    {
        var opl = new ObjectPropertyList(null);
        using (var block = opl.TextBlock())
        {
            // add nothing
        }

        Assert.Empty(Decode(opl));
    }

    [Fact]
    public void Interpolated_AppendsZeroAlloc()
    {
        var opl = new ObjectPropertyList(null);
        var v = 5;
        using (var block = opl.TextBlock())
        {
            block.Add($"Luck Bonus: +{v}%");
        }

        Assert.Equal((1042971, "Luck Bonus: +5%"), Decode(opl)[0]);
    }
}
