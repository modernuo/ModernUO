using Server;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BaseRangedCraftTests
{
    private static PlayerMobile CreatePlayerMobile(Map map, Point3D location)
    {
        var m = new PlayerMobile(World.NewMobile);
        m.DefaultMobileInit();
        m.MoveToWorld(location, map);
        m.AddItem(new Backpack());
        return m;
    }

    private static CraftSystem GetOrInitBowFletchingSystem()
    {
        if (DefBowFletching.CraftSystem == null)
        {
            DefBowFletching.Initialize();
        }

        return DefBowFletching.CraftSystem;
    }

    [Fact]
    public void OnCraft_NullResourceType_UsesRecipeResource()
    {
        Assert.True(Core.ML);

        var player = CreatePlayerMobile(Map.Felucca, new Point3D(4140, 500, 0));
        var bow = new Bow();

        try
        {
            var recipe = new CraftItem(typeof(Bow), "weapons", "bow");
            recipe.AddRes(typeof(Log), "log", 7, "You do not have enough logs.");

            bow.OnCraft(1, false, player, GetOrInitBowFletchingSystem(), null, null, recipe, 0);

            Assert.Equal(CraftResource.RegularWood, bow.Resource);
        }
        finally
        {
            bow.Delete();
            player.Delete();
        }
    }
}
