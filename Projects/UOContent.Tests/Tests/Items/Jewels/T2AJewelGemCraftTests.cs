using Server;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class T2AJewelGemCraftTests
{
    // Y=500 keeps us inside Felucca bounds; X offset avoids other sequential tests.
    private static PlayerMobile CreatePlayerMobile(Map map, Point3D location)
    {
        var m = new PlayerMobile(World.NewMobile);
        m.DefaultMobileInit();
        m.MoveToWorld(location, map);
        m.AddItem(new Backpack());
        return m;
    }

    // Builds a minimal tinkering ring recipe (2 iron ingots) without relying on
    // DefTinkering.InitCraftList, which is gated on the feature flag at startup.
    private static CraftItem MakeRingRecipe()
    {
        var item = new CraftItem(typeof(GoldRing), "ring", "gold ring");
        item.AddRes(typeof(IronIngot), "iron ingot", 2, "You do not have enough ingots.");
        return item;
    }

    // Ensures DefTinkering.CraftSystem is available (not called by the test fixture).
    private static CraftSystem GetOrInitTinkeringSystem()
    {
        if (DefTinkering.CraftSystem == null)
        {
            DefTinkering.Initialize();
        }

        return DefTinkering.CraftSystem;
    }

    [Fact]
    public void OnCraft_ConsumesEntireTargetedGemStack_AndNamesByCount()
    {
        var map = Map.Felucca;
        var player = CreatePlayerMobile(map, new Point3D(4100, 500, 0));

        try
        {
            var pack = player.Backpack;
            pack.AddItem(new IronIngot(10));
            pack.AddItem(new Diamond(50)); // a stack of 50 diamonds

            var system = GetOrInitTinkeringSystem();
            var context = system.GetContext(player);
            context.PendingGemType = GemType.Diamond;
            context.PendingGemCount = 50;

            var ring = new GoldRing();
            ring.OnCraft(1, false, player, system, typeof(IronIngot), null, MakeRingRecipe(), 0);

            Assert.Equal(0, pack.GetAmount(typeof(Diamond)));   // all 50 consumed
            Assert.Equal(GemType.Diamond, ring.GemType);
            Assert.Equal(50, ring.GemCount);
            // pending state cleared so the next craft starts fresh
            Assert.Equal(GemType.None, context.PendingGemType);
            Assert.Equal(0, context.PendingGemCount);
        }
        finally
        {
            player.Delete();
        }
    }

    [Fact]
    public void OnCraft_WithUnsetGemContext_LeavesPlainPiece()
    {
        var map = Map.Felucca;
        var player = CreatePlayerMobile(map, new Point3D(4120, 500, 0));

        try
        {
            player.Backpack.AddItem(new IronIngot(10));

            var system = GetOrInitTinkeringSystem();
            var context = system.GetContext(player);
            context.PendingGemType = GemType.None; // no gem targeted
            context.PendingGemCount = 0;

            var ring = new GoldRing();
            ring.OnCraft(1, false, player, system, typeof(IronIngot), null, MakeRingRecipe(), 0);

            Assert.Equal(GemType.None, ring.GemType);
            Assert.Equal(0, ring.GemCount);
        }
        finally
        {
            player.Delete();
        }
    }
}
