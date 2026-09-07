using System;
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

    // The reported edge case: two properties sharing the same argument. Under the old incremental
    // XOR fold the argument was mixed in twice and cancelled itself out, so "10%" and "5%" produced
    // an identical revision and the client never re-requested the tooltip.
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

    // XOR is self-inverse, so any value mixed in an even number of times vanished entirely --
    // an argument-less property added twice used to leave the hash untouched.
    [Fact]
    public void DuplicateProperty_ChangesHash()
    {
        var once = BuildHash((1060658, null));
        var twice = BuildHash((1060658, null), (1060658, null));

        Assert.NotEqual(once, twice);
    }

    // XOR is commutative, so emission order used to be invisible to the hash even though it is
    // plainly visible in the tooltip.
    [Fact]
    public void PropertyOrder_ChangesHash()
    {
        var forward = BuildHash((1060658, "Alpha"), (1060659, "Beta"));
        var reversed = BuildHash((1060659, "Beta"), (1060658, "Alpha"));

        Assert.NotEqual(forward, reversed);
    }

    // Two properties trading arguments cancelled in exactly the same way as the reported case.
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

    // Terminate writes the bare hash into 0xD6 while SendOPLInfo writes Hash (bit 30 set); the
    // client recovers one from the other by masking off 0x40000000, which only holds while the
    // hash itself stays below that bit. An empty list must also stay non-zero -- the client
    // parks revision 0 as its "nothing cached" sentinel.
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
