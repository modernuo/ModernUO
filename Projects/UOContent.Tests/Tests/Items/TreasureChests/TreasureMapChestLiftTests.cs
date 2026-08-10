using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class TreasureMapChestLiftTests
{
    // Coordinates chosen to avoid overlap with Tracking (1000-4000, 1000-4000) and
    // DetectHidden (1000-2400, 500) test areas.

    [Fact]
    public void PartialLift_MarksSplitRemainderAsLifted()
    {
        using var rng = new PredictableRandom(10); // RandomDouble() = 0.5, no spawn roll fires
        var map = Map.Felucca;
        var location = new Point3D(5000, 600, 0);
        var player = CreatePlayerMobile(map, location);
        var chest = new TreasureMapChest(1);

        try
        {
            chest.MoveToWorld(location, map);
            chest.Locked = false;

            var gold = FindGold(chest, null);
            Assert.NotNull(gold);

            player.Lift(gold, 1, out var rejected, out _);
            Assert.False(rejected);

            // The stack split re-adds the remainder as a brand-new item. It must count as
            // already lifted, otherwise every 1-coin pull grants a fresh guardian spawn roll.
            var remainder = FindGold(chest, gold);
            Assert.NotNull(remainder);
            Assert.Contains(remainder, chest.Lifted);
            Assert.Contains(gold, chest.Lifted);
        }
        finally
        {
            player.Holding?.Delete();
            player.Delete();
            chest.Delete();
        }
    }

    [Fact]
    public void ItemAddedAfterFill_IsMarkedLifted()
    {
        using var rng = new PredictableRandom(10);
        var chest = new TreasureMapChest(1);
        var packed = new Gold(500);

        try
        {
            // Anything entering the chest after the initial fill (packed-back gold, split
            // remainders, GM drops) was never part of the original loot and must not
            // grant spawn rolls when lifted back out.
            chest.DropItem(packed);

            Assert.Contains(packed, chest.Lifted);
        }
        finally
        {
            chest.Delete();
        }
    }

    [Fact]
    public void OriginalFillLoot_IsNotMarkedLifted()
    {
        using var rng = new PredictableRandom(10);
        var chest = new TreasureMapChest(1);

        try
        {
            // The original loot must stay roll-eligible for its first lift.
            Assert.True(chest.Lifted == null || chest.Lifted.Count == 0);
        }
        finally
        {
            chest.Delete();
        }
    }

    private static Gold FindGold(TreasureMapChest chest, Gold except)
    {
        var items = chest.Items;

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is Gold gold && gold != except)
            {
                return gold;
            }
        }

        return null;
    }

    private static PlayerMobile CreatePlayerMobile(Map map, Point3D location)
    {
        var mobile = new PlayerMobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.MoveToWorld(location, map);
        return mobile;
    }
}
