using System;
using System.Collections.Generic;
using Server.Items;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class ContainerTests
{
    [Theory]
    [InlineData(typeof(Container))]
    [InlineData(typeof(Item))]
    public void TestFindItemsByType(Type itemType)
    {
        var staticSerial = (Serial)0x3;

        var container = itemType.CreateInstance<Item>((Serial)0x1);
        container.AddItem(new Item((Serial)0x2));
        container.AddItem(new Static(staticSerial));

        Static staticItem = null;
        foreach (var item in container.FindItemsByType<Static>())
        {
            staticItem = item;
        }

        Assert.NotNull(staticItem);
        Assert.Equal(staticSerial, staticItem.Serial);
    }

    [Theory]
    [InlineData(typeof(Container))]
    [InlineData(typeof(Item))]
    public void TestFindItemsByTypeNested(Type itemType)
    {
        var static1 = new Static((Serial)0x3);
        var static2 = new Static((Serial)0x6);

        var container = itemType.CreateInstance<Item>((Serial)0x1);
        container.AddItem(new Item((Serial)0x2));
        var container2 = itemType.CreateInstance<Item>((Serial)0x4);
        container.AddItem(container2);

        var container3 = itemType.CreateInstance<Item>((Serial)0x5);
        container2.AddItem(container3);
        container3.AddItem(static2);

        container2.AddItem(static1);

        var statics = new List<Static>();
        foreach (var item in container.FindItemsByType<Static>())
        {
            statics.Add(item);
        }

        Assert.Equal(2, statics.Count);
        Assert.Equal(static1, statics[0]);
        Assert.Equal(static2, statics[1]);
    }

    [Theory]
    [InlineData(typeof(Container))]
    [InlineData(typeof(Item))]
    public void TestFindItemsByTypeNotMatching(Type itemType)
    {
        var container = itemType.CreateInstance<Item>((Serial)0x1);
        container.AddItem(new Item((Serial)0x2));
        var container2 = itemType.CreateInstance<Item>((Serial)0x4);
        container.AddItem(container2);
        container2.AddItem(new Item((Serial)0x5));

        Static staticItem = null;
        foreach (var item in container.FindItemsByType<Static>())
        {
            staticItem = item;
        }

        Assert.Null(staticItem);
    }

    [Theory]
    [InlineData(typeof(Container))]
    [InlineData(typeof(Item))]
    public void TestFindItemsByTypeShouldThrowWhenModified(Type itemType)
    {
        var container = itemType.CreateInstance<Item>((Serial)0x1);
        container.AddItem(new Item((Serial)0x2));
        var staticItem = new Static((Serial)0x3);
        container.AddItem(staticItem);
        container.AddItem(new Item((Serial)0x4));

        Assert.Throws<InvalidOperationException>(
            () =>
            {
                foreach (var item in container.FindItemsByType<Static>())
                {
                    if (item == staticItem)
                    {
                        container.RemoveItem(staticItem);
                    }
                }
            }
        );
    }

    [Theory]
    [InlineData(typeof(Container))]
    [InlineData(typeof(Item))]
    public void TestEnumerateItemsByTypeWhenModified(Type itemType)
    {
        var container = itemType.CreateInstance<Item>((Serial)0x1);

        var item1 = new Item((Serial)0x2);
        container.AddItem(item1);
        var item2 = new Static((Serial)0x3);
        container.AddItem(item2);
        var item3 = new Item((Serial)0x4);
        container.AddItem(item3);

        foreach (var item in container.EnumerateItemsByType<Static>())
        {
            if (item == item2)
            {
                container.RemoveItem(item2);
            }
        }

        Assert.Equal(2, container.Items.Count);
        Assert.Collection(container.Items,
            item => Assert.Equal(item1, item),
            item => Assert.Equal(item3, item)
        );
    }

    // -------------------------------------------------------------------------
    // ConsumeTotal / ConsumeTotalGrouped / GetBestGroupAmount / GetAmount /
    // FindItemByType / ConsumeUpTo behavior locks.
    //
    // These tests pin down the public contract before the optimization pass:
    //   - all-or-nothing semantics across multi-slot consume
    //   - callback ordering and per-item delta values
    //   - "first sufficient group wins" (not best) for ConsumeTotalGrouped
    //   - grouper(a, b) receives the group leader as `a`
    //   - >= amount threshold for groups
    // -------------------------------------------------------------------------

    private static Container MakeContainer(uint serial = 0x100) => new((Serial)serial);

    private static Item MakeStack(uint serial, int amount, int hue = 0)
    {
        var item = new Item((Serial)serial) { Amount = amount };
        if (hue != 0)
        {
            item.Hue = hue;
        }
        return item;
    }

    private static TestReagentA MakeReagentA(uint serial, int amount, int hue = 0)
    {
        var item = new TestReagentA((Serial)serial) { Amount = amount };
        if (hue != 0)
        {
            item.Hue = hue;
        }
        return item;
    }

    private static TestReagentB MakeReagentB(uint serial, int amount, int hue = 0)
    {
        var item = new TestReagentB((Serial)serial) { Amount = amount };
        if (hue != 0)
        {
            item.Hue = hue;
        }
        return item;
    }

    // Mirrors CraftItem.CheckHueGrouping: groups items that share a hue.
    private static int HueGrouper(Item a, Item b) => b.Hue.CompareTo(a.Hue);

    [Fact]
    public void TestConsumeTotal_SingleType_ExactAmount_Succeeds()
    {
        var c = MakeContainer(0x200);
        c.AddItem(MakeReagentA(0x201, 5));

        Assert.True(c.ConsumeTotal(typeof(TestReagentA), 5));
        Assert.Empty(c.Items); // Stack was fully consumed -> Delete()
    }

    [Fact]
    public void TestConsumeTotal_SingleType_AcrossStacks_PartialOnLast()
    {
        var c = MakeContainer(0x210);
        var a = MakeReagentA(0x211, 3);
        var b = MakeReagentA(0x212, 4);
        c.AddItem(a);
        c.AddItem(b);

        Assert.True(c.ConsumeTotal(typeof(TestReagentA), 5));
        // BFS order: a fully consumed (3), b partially (2 of 4 remain).
        Assert.True(a.Deleted);
        Assert.False(b.Deleted);
        Assert.Equal(2, b.Amount);
    }

    [Fact]
    public void TestConsumeTotal_SingleType_NotEnough_NoConsumption()
    {
        var c = MakeContainer(0x220);
        var a = MakeReagentA(0x221, 2);
        var b = MakeReagentA(0x222, 2);
        c.AddItem(a);
        c.AddItem(b);

        Assert.False(c.ConsumeTotal(typeof(TestReagentA), 10));
        // Must not partially consume.
        Assert.Equal(2, a.Amount);
        Assert.Equal(2, b.Amount);
    }

    [Fact]
    public void TestConsumeTotal_MultiType_FailsAtSlot1_Slot0Untouched()
    {
        var c = MakeContainer(0x230);
        var ra = MakeReagentA(0x231, 5);
        var rb = MakeReagentB(0x232, 1); // not enough for slot 1
        c.AddItem(ra);
        c.AddItem(rb);

        var result = c.ConsumeTotal(
            new[] { typeof(TestReagentA), typeof(TestReagentB) },
            new[] { 5, 5 }
        );

        Assert.Equal(1, result);
        // Critical all-or-nothing invariant.
        Assert.Equal(5, ra.Amount);
        Assert.Equal(1, rb.Amount);
    }

    [Fact]
    public void TestConsumeTotal_NestedContainer_RecurseCountsChildItems()
    {
        var outer = MakeContainer(0x240);
        var inner = MakeContainer(0x241);
        outer.AddItem(inner);
        inner.AddItem(MakeReagentA(0x242, 4));
        outer.AddItem(MakeReagentA(0x243, 1));

        Assert.True(outer.ConsumeTotal(typeof(TestReagentA), 5));
        // Both reagents deleted; inner container itself persists in outer.
        Assert.Single(outer.Items);
        Assert.Empty(inner.Items);
    }

    [Fact]
    public void TestConsumeTotal_CallbackFires_PerItem_WithDelta()
    {
        var c = MakeContainer(0x250);
        c.AddItem(MakeReagentA(0x251, 3));
        c.AddItem(MakeReagentA(0x252, 4));

        var calls = new List<(int serial, int amount)>();
        Assert.True(
            c.ConsumeTotal(
                typeof(TestReagentA), 5, true,
                (item, amt) => calls.Add(((int)item.Serial.Value, amt))
            )
        );

        Assert.Equal(2, calls.Count);
        Assert.Equal((0x251, 3), calls[0]); // full first stack
        Assert.Equal((0x252, 2), calls[1]); // partial second stack
    }

    [Fact]
    public void TestConsumeTotalGrouped_HueGrouping_FirstSufficientGroupWins()
    {
        // Two hue groups: blue (sum=4, insufficient), red (sum=8, sufficient).
        // First group meeting >= amount in BFS order wins. Blue is first; it's
        // skipped because insufficient. Red wins. Verify red consumed, blue intact.
        var c = MakeContainer(0x260);
        var blue1 = MakeReagentA(0x261, 2, hue: 0x10);
        var blue2 = MakeReagentA(0x262, 2, hue: 0x10);
        var red1 = MakeReagentA(0x263, 5, hue: 0x20);
        var red2 = MakeReagentA(0x264, 3, hue: 0x20);
        c.AddItem(blue1);
        c.AddItem(blue2);
        c.AddItem(red1);
        c.AddItem(red2);

        var result = c.ConsumeTotalGrouped(
            new[] { typeof(TestReagentA) }, new[] { 6 },
            true, null, HueGrouper
        );

        Assert.Equal(-1, result);
        Assert.Equal(2, blue1.Amount);
        Assert.Equal(2, blue2.Amount);
        // Red consumed: 5 + 1 = 6 needed.
        Assert.True(red1.Deleted);
        Assert.False(red2.Deleted);
        Assert.Equal(2, red2.Amount);
    }

    [Fact]
    public void TestConsumeTotalGrouped_GrouperReceivesGroupLeader()
    {
        // grouper(a, b) must receive the group's first item as `a`, not the
        // previous item. This matters when grouping is order-sensitive.
        var c = MakeContainer(0x270);
        var i1 = MakeReagentA(0x271, 1, hue: 0x10);
        var i2 = MakeReagentA(0x272, 1, hue: 0x10);
        var i3 = MakeReagentA(0x273, 1, hue: 0x10);
        c.AddItem(i1);
        c.AddItem(i2);
        c.AddItem(i3);

        var leaderSerials = new List<int>();
        c.ConsumeTotalGrouped(
            new[] { typeof(TestReagentA) }, new[] { 3 },
            true, null,
            (a, b) =>
            {
                leaderSerials.Add((int)a.Serial.Value);
                return b.Hue.CompareTo(a.Hue);
            }
        );

        // Leader for every comparison in the single group must be i1 (the first).
        Assert.NotEmpty(leaderSerials);
        Assert.All(leaderSerials, s => Assert.Equal((int)i1.Serial.Value, s));
    }

    [Fact]
    public void TestConsumeTotalGrouped_PartialFailure_NothingConsumedFromAnySlot()
    {
        // Slot 0 has enough; slot 1 does not. Nothing consumed, return 1.
        var c = MakeContainer(0x280);
        var a = MakeReagentA(0x281, 5, hue: 0x10);
        var b = MakeReagentB(0x282, 1, hue: 0x10);
        c.AddItem(a);
        c.AddItem(b);

        var result = c.ConsumeTotalGrouped(
            new[] { typeof(TestReagentA), typeof(TestReagentB) },
            new[] { 5, 5 },
            true, null, HueGrouper
        );

        Assert.Equal(1, result);
        Assert.Equal(5, a.Amount);
        Assert.Equal(1, b.Amount);
    }

    [Fact]
    public void TestConsumeTotalGrouped_ExactGroupAmount_Succeeds()
    {
        // Group sum equals exactly the requested amount. Threshold is >=, so
        // a group of exactly N satisfies a request for N.
        var c = MakeContainer(0x330);
        var stack = MakeReagentA(0x331, 5, hue: 0x10);
        c.AddItem(stack);

        var result = c.ConsumeTotalGrouped(
            new[] { typeof(TestReagentA) }, new[] { 5 },
            true, null, HueGrouper
        );

        Assert.Equal(-1, result);
        Assert.True(stack.Deleted);
    }

    [Fact]
    public void TestConsumeTotalGrouped_CallbackOrderMatchesBFS()
    {
        // OnResourceConsumed in CraftItem.cs retains the hue of the largest
        // consumed stack, so callback order must match BFS iteration order.
        var c = MakeContainer(0x290);
        c.AddItem(MakeReagentA(0x291, 3, hue: 0x10));
        c.AddItem(MakeReagentA(0x292, 4, hue: 0x10));

        var calls = new List<(int serial, int amount)>();
        c.ConsumeTotalGrouped(
            new[] { typeof(TestReagentA) }, new[] { 5 },
            true,
            (item, amt) => calls.Add(((int)item.Serial.Value, amt)),
            HueGrouper
        );

        Assert.Equal(2, calls.Count);
        Assert.Equal((0x291, 3), calls[0]);
        Assert.Equal((0x292, 2), calls[1]);
    }

    [Fact]
    public void TestGetBestGroupAmount_ReturnsLargestGroupSum()
    {
        var c = MakeContainer(0x2A0);
        c.AddItem(MakeReagentA(0x2A1, 2, hue: 0x10));
        c.AddItem(MakeReagentA(0x2A2, 2, hue: 0x10));
        c.AddItem(MakeReagentA(0x2A3, 5, hue: 0x20));
        c.AddItem(MakeReagentA(0x2A4, 3, hue: 0x20));

        var best = c.GetBestGroupAmount(new[] { typeof(TestReagentA) }, true, HueGrouper);
        Assert.Equal(8, best);
    }

    [Fact]
    public void TestGetBestGroupAmount_EmptyOrNoMatch_ReturnsZero()
    {
        var c = MakeContainer(0x2B0);
        Assert.Equal(0, c.GetBestGroupAmount(new[] { typeof(TestReagentA) }, true, HueGrouper));

        c.AddItem(MakeReagentB(0x2B1, 5));
        Assert.Equal(0, c.GetBestGroupAmount(new[] { typeof(TestReagentA) }, true, HueGrouper));
    }

    [Fact]
    public void TestGetBestGroupAmount_NullGrouper_Throws()
    {
        var c = MakeContainer(0x2C0);
        Assert.Throws<ArgumentNullException>(
            () => c.GetBestGroupAmount(new[] { typeof(TestReagentA) }, true, null)
        );
    }

    [Fact]
    public void TestGetAmount_TypeArray_SumsAcrossTypes()
    {
        var c = MakeContainer(0x2D0);
        c.AddItem(MakeReagentA(0x2D1, 3));
        c.AddItem(MakeReagentB(0x2D2, 4));
        c.AddItem(MakeStack(0x2D3, 99)); // base Item, doesn't match A or B

        var total = c.GetAmount(new[] { typeof(TestReagentA), typeof(TestReagentB) });
        Assert.Equal(7, total);
    }

    [Fact]
    public void TestFindItemByType_Generic_WithPredicate()
    {
        var c = MakeContainer(0x2E0);
        c.AddItem(MakeReagentA(0x2E1, 1, hue: 0x10));
        var target = MakeReagentA(0x2E2, 1, hue: 0x20);
        c.AddItem(target);
        c.AddItem(MakeReagentA(0x2E3, 1, hue: 0x10));

        var found = c.FindItemByType<TestReagentA>(true, item => item.Hue == 0x20);
        Assert.Same(target, found);
    }

    [Fact]
    public void TestConsumeUpTo_DeletesExhaustedStacks()
    {
        var c = MakeContainer(0x2F0);
        var s1 = MakeReagentA(0x2F1, 3);
        var s2 = MakeReagentA(0x2F2, 3);
        c.AddItem(s1);
        c.AddItem(s2);

        var consumed = c.ConsumeUpTo(typeof(TestReagentA), 5);
        Assert.Equal(5, consumed);
        // BFS: first stack fully (3), second partially (2 of 3 remain).
        Assert.True(s1.Deleted);
        Assert.False(s2.Deleted);
        Assert.Equal(1, s2.Amount);
    }

    [Fact]
    public void TestConsumeTotal_LengthMismatch_Throws()
    {
        var c = MakeContainer(0x300);
        Assert.Throws<ArgumentException>(
            () => c.ConsumeTotal(new[] { typeof(TestReagentA) }, new[] { 1, 2 })
        );
    }

    [Fact]
    public void TestConsumeTotalGrouped_LengthMismatch_Throws()
    {
        var c = MakeContainer(0x310);
        Assert.Throws<ArgumentException>(
            () => c.ConsumeTotalGrouped(
                new[] { typeof(TestReagentA) }, new[] { 1, 2 },
                true, null, HueGrouper
            )
        );
    }

    [Fact]
    public void TestConsumeTotalGrouped_NullGrouper_Throws()
    {
        var c = MakeContainer(0x320);
        Assert.Throws<ArgumentNullException>(
            () => c.ConsumeTotalGrouped(
                new[] { typeof(TestReagentA) }, new[] { 1 },
                true, null, null
            )
        );
    }

    // -------------------------------------------------------------------------
    // FindItemsByType(Type) and FindItemsByType(ReadOnlySpan<Type>) — Phase 10:
    // these used to allocate a Predicate<Item> per call (method-group conversion
    // for the single-Type case, real closure for the Type[] case). The
    // enumerator now stores the filter directly.
    // -------------------------------------------------------------------------

    [Fact]
    public void TestFindItemsByType_RuntimeType_MatchesPredicatePath()
    {
        var c = MakeContainer(0x340);
        c.AddItem(MakeReagentA(0x341, 1));
        c.AddItem(MakeReagentB(0x342, 1));
        c.AddItem(MakeReagentA(0x343, 1));
        c.AddItem(MakeStack(0x344, 1)); // base Item, doesn't match TestReagentA

        var fromRuntime = new List<int>();
        foreach (var item in c.FindItemsByType(typeof(TestReagentA)))
        {
            fromRuntime.Add((int)item.Serial.Value);
        }

        var fromGeneric = new List<int>();
        foreach (var item in c.FindItemsByType<TestReagentA>())
        {
            fromGeneric.Add((int)item.Serial.Value);
        }

        Assert.Equal(fromGeneric, fromRuntime);
        Assert.Equal(new[] { 0x341, 0x343 }, fromRuntime);
    }

    [Fact]
    public void TestFindItemsByType_TypeSpan_MatchesUnionOfTypes()
    {
        var c = MakeContainer(0x350);
        c.AddItem(MakeReagentA(0x351, 1));
        c.AddItem(MakeReagentB(0x352, 1));
        c.AddItem(MakeStack(0x353, 1)); // base Item

        var serials = new List<int>();
        foreach (var item in c.FindItemsByType(new[] { typeof(TestReagentA), typeof(TestReagentB) }))
        {
            serials.Add((int)item.Serial.Value);
        }

        Assert.Equal(new[] { 0x351, 0x352 }, serials);
    }

    [Fact]
    public void TestFindItemsByType_RuntimeType_NoAllocations()
    {
        // Establishes that the (Type) path no longer allocates a Predicate per
        // call. Snapshot allocation counter, run a few iterations, assert flat.
        var c = MakeContainer(0x360);
        c.AddItem(MakeReagentA(0x361, 1));
        c.AddItem(MakeReagentA(0x362, 1));

        // Warm up to load any first-call jitting.
        foreach (var _ in c.FindItemsByType(typeof(TestReagentA))) { }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
        {
            foreach (var _ in c.FindItemsByType(typeof(TestReagentA)) ) { }
        }
        var delta = GC.GetAllocatedBytesForCurrentThread() - before;

        // PooledRefQueue rents from the pool but the rental itself doesn't
        // allocate when the bucket is warm. Allow a small headroom for any
        // first-rent-after-pool-cleanup allocations but well below the ~48
        // bytes/call the old delegate path would have produced (=4800 bytes).
        Assert.True(delta < 1024, $"Expected near-zero allocations, got {delta} bytes across 100 iterations");
    }

    [Fact]
    public void TestFindItemsByType_TypeSpan_NoAllocations()
    {
        var c = MakeContainer(0x370);
        c.AddItem(MakeReagentA(0x371, 1));
        c.AddItem(MakeReagentB(0x372, 1));

        var types = new[] { typeof(TestReagentA), typeof(TestReagentB) };
        foreach (var _ in c.FindItemsByType(types)) { } // warm

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
        {
            foreach (var _ in c.FindItemsByType(types)) { }
        }
        var delta = GC.GetAllocatedBytesForCurrentThread() - before;

        // Old closure path: ~80 bytes/call → ~8000 bytes for 100 iterations.
        Assert.True(delta < 1024, $"Expected near-zero allocations, got {delta} bytes across 100 iterations");
    }
}

// Test-only Item subclasses used to differentiate concrete types in the
// consume/find/group tests above. They have no serialization generator
// (the tests never round-trip through World). Stackable is set so the Amount
// setter doesn't log "Amount changed for non-stackable item" warnings.
public sealed class TestReagentA : Item
{
    public TestReagentA(Serial serial) : base(serial) => Stackable = true;
}

public sealed class TestReagentB : Item
{
    public TestReagentB(Serial serial) : base(serial) => Stackable = true;
}
