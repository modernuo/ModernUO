using Server;
using Server.Engines.Craft;
using Server.Engines.Craft.T2A;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class T2AHuedFilterTests
{
    private static PlayerMobile CreatePlayer(Point3D location)
    {
        var m = new PlayerMobile(World.NewMobile);
        m.DefaultMobileInit();
        m.MoveToWorld(location, Map.Felucca);
        m.AddItem(new Backpack());
        return m;
    }

    private static CraftItem MakeRecipe()
    {
        var item = new CraftItem(typeof(Shirt), "shirt", "shirt");
        item.AddRes(typeof(Cloth), "cloth", 8, "You do not have enough cloth.");
        item.AddSkill(SkillName.Tailoring, 0.0, 100.0);
        return item;
    }

    private static CraftSystem GetTailoringSystem()
    {
        if (DefTailoring.CraftSystem == null)
        {
            DefTailoring.Initialize();
        }

        return DefTailoring.CraftSystem;
    }

    [Fact]
    public void CanCraftItem_WithHue_CountsOnlyMatchingHueCloth()
    {
        var player = CreatePlayer(new Point3D(4140, 500, 0));

        try
        {
            var pack = player.Backpack;
            pack.AddItem(new Cloth(5) { Hue = 0x21 });
            pack.AddItem(new Cloth(5) { Hue = 0x30 });

            var system = GetTailoringSystem();
            var recipe = MakeRecipe();

            // 10 cloth total, but no single hue reaches the 8 the recipe needs
            Assert.True(T2ACraftSystem.CanCraftItem(player, recipe, system));
            Assert.False(T2ACraftSystem.CanCraftItem(player, recipe, system, null, 0x21));
            Assert.False(T2ACraftSystem.CanCraftItem(player, recipe, system, null, 0x30));

            pack.AddItem(new Cloth(3) { Hue = 0x21 });
            Assert.True(T2ACraftSystem.CanCraftItem(player, recipe, system, null, 0x21));
            Assert.False(T2ACraftSystem.CanCraftItem(player, recipe, system, null, 0x30));
        }
        finally
        {
            player.Delete();
        }
    }

    [Fact]
    public void CanCraftItem_WithHue_CountsSameHueUncutCloth()
    {
        var player = CreatePlayer(new Point3D(4140, 500, 0));

        try
        {
            var pack = player.Backpack;
            pack.AddItem(new Cloth(5) { Hue = 0x21 });
            pack.AddItem(new UncutCloth(3) { Hue = 0x21 });
            pack.AddItem(new UncutCloth(5) { Hue = 0x30 });

            var system = GetTailoringSystem();
            var recipe = MakeRecipe();

            Assert.True(T2ACraftSystem.CanCraftItem(player, recipe, system, null, 0x21));
            Assert.False(T2ACraftSystem.CanCraftItem(player, recipe, system, null, 0x30));
        }
        finally
        {
            player.Delete();
        }
    }
}
