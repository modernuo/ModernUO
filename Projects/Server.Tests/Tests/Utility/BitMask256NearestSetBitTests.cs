using Xunit;

namespace Server.Tests;

public class BitMask256NearestSetBitTests
{
    private static BitMask256 MaskOf(params int[] bits)
    {
        var mask = BitMask256.AllClear();
        foreach (var bit in bits)
        {
            mask.SetBit(bit);
        }

        return mask;
    }

    [Fact]
    public void EmptyMaskReturnsMinusOne()
    {
        Assert.Equal(-1, BitMask256.AllClear().NearestSetBit(100));
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(5, 5)]
    [InlineData(20, 5)]
    [InlineData(30, 40)]
    [InlineData(39, 40)]
    [InlineData(255, 200)]
    public void PicksClosestSetBit(int target, int expected)
    {
        Assert.Equal(expected, MaskOf(5, 40, 200).NearestSetBit(target));
    }

    [Fact]
    public void TieGoesToTheLowerBit()
    {
        Assert.Equal(10, MaskOf(10, 20).NearestSetBit(15));
    }

    [Theory]
    [InlineData(63, 64)]
    [InlineData(64, 64)]
    [InlineData(127, 128)]
    [InlineData(191, 192)]
    public void FindsBitsAcrossSegmentBoundaries(int target, int expected)
    {
        Assert.Equal(expected, MaskOf(0, expected).NearestSetBit(target));
    }

    [Theory]
    [InlineData(-50)]
    [InlineData(1000)]
    public void TargetOutsideRangeIsClamped(int target)
    {
        var mask = MaskOf(3, 250);
        Assert.Equal(target < 0 ? 3 : 250, mask.NearestSetBit(target));
    }

    [Fact]
    public void TargetZeroMatchesLowestSetBit()
    {
        var mask = MaskOf(17, 99, 180);
        Assert.Equal(mask.LowestSetBit(), mask.NearestSetBit(0));
    }

    [Fact]
    public void DoesNotModifyTheMask()
    {
        var mask = MaskOf(1, 100, 254);
        mask.NearestSetBit(100);
        Assert.Equal(3, mask.PopCount());
    }
}
