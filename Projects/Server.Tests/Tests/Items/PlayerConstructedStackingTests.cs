using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class PlayerConstructedStackingTests
{
    // PlayerConstructed is per-instance provenance, and stack operations were written when no
    // item carried any. Merging keeps the receiver's copy of a field and splitting rebuilds one
    // half from a fixed list of fields, so a flag that is not accounted for in both places is
    // one that ordinary stacking can launder or erase.

    // Stands in for a real stackable type. LiftItemDupe builds the remainder through the
    // parameterless constructor and copies only a fixed list of fields onto it -- Stackable is
    // not on that list -- so the remainder is only stackable if the type restores it the way
    // every genuine stackable does.
    private class StackableItem : Item
    {
        public StackableItem() => Stackable = true;

        public StackableItem(Serial serial) : base(serial) => Stackable = true;
    }

    private static StackableItem MakeStack(Serial serial, int amount, bool playerConstructed) =>
        new(serial) { Amount = amount, PlayerConstructed = playerConstructed };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanStackWith_IsTrueWhenProvenanceMatches(bool playerConstructed)
    {
        var first = MakeStack((Serial)0x1, 5, playerConstructed);
        var second = MakeStack((Serial)0x2, 7, playerConstructed);

        try
        {
            Assert.True(first.CanStackWith(second));
        }
        finally
        {
            first.Delete();
            second.Delete();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LiftItemDupe_CopiesPlayerConstructedToRemainder(bool playerConstructed)
    {
        var stack = MakeStack((Serial)0x1, 10, playerConstructed);
        Item remainder = null;

        try
        {
            remainder = Mobile.LiftItemDupe(stack, 4);

            Assert.NotNull(remainder);
            Assert.NotSame(stack, remainder);
            Assert.Equal(4, stack.Amount);
            Assert.Equal(6, remainder.Amount);
            Assert.Equal(playerConstructed, remainder.PlayerConstructed);
        }
        finally
        {
            stack.Delete();
            remainder?.Delete();
        }
    }

    [Fact]
    public void SplitHalvesRemainStackableWithEachOther()
    {
        // The two halves of a split must still be one pile's worth: if the split dropped the
        // flag, the remainder would no longer stack back onto what it came from.
        var stack = MakeStack((Serial)0x1, 10, true);
        Item remainder = null;

        try
        {
            remainder = Mobile.LiftItemDupe(stack, 4);
            Assert.NotNull(remainder);

            Assert.True(stack.CanStackWith(remainder));
            Assert.True(stack.StackWith(null, remainder, false));
            Assert.Equal(10, stack.Amount);
            Assert.True(stack.PlayerConstructed);
        }
        finally
        {
            stack.Delete();
            remainder?.Delete();
        }
    }
}
