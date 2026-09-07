using System;
using System.Collections.Generic;
using Server;
using Xunit;

namespace Server.Tests;

public class ObjectPropertyListHashTests
{
    private static int BuildHash(params (int cliloc, string arg)[] properties)
    {
        var opl = new ObjectPropertyList(null);

        foreach (var (cliloc, arg) in properties)
        {
            if (arg == null)
            {
                opl.Add(cliloc);
            }
            else
            {
                opl.Add(cliloc, arg.AsSpan());
            }
        }

        opl.Terminate();
        return opl.Hash;
    }

    // Two properties sharing an argument: the XOR fold mixed it in twice and cancelled it, so
    // "10%" and "5%" produced the same revision and the client kept the stale tooltip.
    [Fact]
    public void RepeatedArgument_DoesNotCancelOut()
    {
        var ten = BuildHash(
            (1063752, "10"),
            (1063737, "10"),
            (1063740, null)
        );

        var five = BuildHash(
            (1063752, "5"),
            (1063737, "5"),
            (1063740, null)
        );

        Assert.NotEqual(ten, five);
    }

    // XOR is self-inverse: any value mixed in an even number of times vanished.
    [Fact]
    public void DuplicateProperty_ChangesHash()
    {
        var once = BuildHash((1060658, null));
        var twice = BuildHash((1060658, null), (1060658, null));

        Assert.NotEqual(once, twice);
    }

    // XOR is commutative, so emission order was invisible to the hash but visible in the tooltip.
    [Fact]
    public void PropertyOrder_ChangesHash()
    {
        var forward = BuildHash((1060658, "Alpha"), (1060659, "Beta"));
        var reversed = BuildHash((1060659, "Beta"), (1060658, "Alpha"));

        Assert.NotEqual(forward, reversed);
    }

    [Fact]
    public void SwappedArguments_ChangeHash()
    {
        var forward = BuildHash((1063752, "10"), (1063737, "5"));
        var swapped = BuildHash((1063752, "5"), (1063737, "10"));

        Assert.NotEqual(forward, swapped);
    }

    [Fact]
    public void IdenticalContent_ProducesIdenticalHash()
    {
        var first = BuildHash((1063752, "10"), (1063737, "5"), (1063740, null));
        var second = BuildHash((1063752, "10"), (1063737, "5"), (1063740, null));

        Assert.Equal(first, second);
    }

    // The client masks 0x40000000 off the 0xDC revision to match the 0xD6 hash, so the hash has
    // to stay below that bit.
    [Fact]
    public void Hash_StaysWithinTheRevisionMask()
    {
        var opl = new ObjectPropertyList(null);
        opl.Add(1063752, "A rather long argument that pushes the buffer past its initial size".AsSpan());
        opl.Add(1063737, "12345");
        opl.Add(1063740);
        opl.Terminate();

        Assert.Equal(0x40000000, opl.Hash & ~0x3FFFFFF);
    }

    // 6- and 8-byte blocks take xxHash3's short-input paths. They still avalanche across all 26
    // kept bits, so a counter ticking down never repeats the revision it just had.
    [Fact]
    public void ShortNumericArguments_ConsecutiveValuesDiffer()
    {
        var previous = BuildHash((1060584, "0"));

        for (var charges = 1; charges < 20000; charges++)
        {
            var current = BuildHash((1060584, charges.ToString()));
            Assert.NotEqual(previous, current);
            previous = current;
        }
    }

    [Fact]
    public void SmallPropertyBlocks_StayWellDistributed()
    {
        const int count = 20000;

        var withArgument = new HashSet<int>();
        var withoutArgument = new HashSet<int>();

        for (var i = 0; i < count; i++)
        {
            withArgument.Add(BuildHash((1060584, i.ToString())));
            withoutArgument.Add(BuildHash((1060000 + i, null)));
        }

        // Birthday expects ~3 collisions over a 26-bit space; allow an order of magnitude so the
        // bound holds for any seed. A hash that stopped mixing collapses far past it.
        Assert.True(withArgument.Count >= count - 30, $"8-byte blocks: {withArgument.Count}/{count}");
        Assert.True(withoutArgument.Count >= count - 30, $"6-byte blocks: {withoutArgument.Count}/{count}");
    }

    [Fact]
    public void EmptyList_IsNonZeroAndDistinctFromPopulated()
    {
        var empty = new ObjectPropertyList(null);
        empty.Terminate();

        Assert.NotEqual(0, empty.Hash);
        Assert.Equal(0x40000000, empty.Hash & ~0x3FFFFFF);
        Assert.NotEqual(empty.Hash, BuildHash((1060658, null)));
    }
}
