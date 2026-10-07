using Server;
using Server.Engines.Craft.T2A;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class T2ACookingTests
{
    private static PlayerMobile CreatePlayer()
    {
        var m = new PlayerMobile(World.NewMobile);
        m.DefaultMobileInit();
        m.MoveToWorld(new Point3D(4200, 500, 0), Map.Felucca);
        m.AddItem(new Backpack());
        return m;
    }

    [Fact]
    public void FlourAndWaterPitcher_MakeDough()
    {
        var player = CreatePlayer();
        var pack = player.Backpack;
        var sack = new SackFlour { ItemID = 0x103A };
        var pitcher = new Pitcher(BeverageType.Water);
        pack.AddItem(sack);
        pack.AddItem(pitcher);

        var sackBefore = sack.Quantity;
        var pitcherBefore = pitcher.Quantity;

        Assert.True(T2ACooking.TryCombine(player, sack, pitcher));
        Assert.Equal(sackBefore - 1, sack.Quantity);
        Assert.Equal(pitcherBefore - 1, pitcher.Quantity);
        Assert.Equal(1, pack.GetAmount(typeof(Dough)));
    }

    [Fact]
    public void ClosedFlourSack_IsRefused()
    {
        var player = CreatePlayer();
        var sack = new SackFlour();
        var pitcher = new Pitcher(BeverageType.Water);
        player.Backpack.AddItem(sack);
        player.Backpack.AddItem(pitcher);

        Assert.False(T2ACooking.TryCombine(player, sack, pitcher));
        Assert.Equal(20, sack.Quantity);
        Assert.Equal(pitcher.MaxQuantity, pitcher.Quantity);
        Assert.Equal(0, player.Backpack.GetAmount(typeof(Dough)));
    }

    [Fact]
    public void DoughAndApple_MakeUnbakedApplePie()
    {
        var player = CreatePlayer();
        var dough = new Dough();
        var apple = new Apple();
        player.Backpack.AddItem(dough);
        player.Backpack.AddItem(apple);

        Assert.True(T2ACooking.TryCombine(player, dough, apple));
        Assert.Equal(1, player.Backpack.GetAmount(typeof(UnbakedApplePie)));
        Assert.Equal(0, player.Backpack.GetAmount(typeof(Apple)));
        Assert.Equal(0, player.Backpack.GetAmount(typeof(Dough)));
    }

    [Fact]
    public void HoneyRows_WorkInEitherDirection()
    {
        var player = CreatePlayer();
        var pack = player.Backpack;
        var dough = new Dough();
        var honey = new JarHoney();
        pack.AddItem(dough);
        pack.AddItem(honey);
        Assert.True(T2ACooking.TryCombine(player, dough, honey));
        Assert.Equal(1, pack.GetAmount(typeof(SweetDough)));

        var dough2 = new Dough();
        var honey2 = new JarHoney();
        pack.AddItem(dough2);
        pack.AddItem(honey2);
        Assert.True(T2ACooking.TryCombine(player, honey2, dough2));
        Assert.Equal(2, pack.GetAmount(typeof(SweetDough)));

        var sweet = pack.FindItemByType<SweetDough>();
        var honey3 = new JarHoney();
        pack.AddItem(honey3);
        Assert.True(T2ACooking.TryCombine(player, sweet, honey3));
        Assert.Equal(1, pack.GetAmount(typeof(CookieMix)));
    }

    [Fact]
    public void WrongTarget_ConsumesNothing()
    {
        var player = CreatePlayer();
        var dough = new Dough();
        var honey = new JarHoney();
        player.Backpack.AddItem(dough);
        player.Backpack.AddItem(honey);
        var apple = new Apple();
        player.Backpack.AddItem(apple);

        Assert.False(T2ACooking.TryCombine(player, honey, apple));
        Assert.False(T2ACooking.TryCombine(player, dough, new Pitcher(BeverageType.Water)));
        Assert.Equal(1, player.Backpack.GetAmount(typeof(Dough)));
        Assert.Equal(1, player.Backpack.GetAmount(typeof(JarHoney)));
        Assert.Equal(1, player.Backpack.GetAmount(typeof(Apple)));
    }

    [Fact]
    public void TryBeginUse_ReturnsFalseForUnrelatedItem()
    {
        var player = CreatePlayer();
        var apple = new Apple();
        player.Backpack.AddItem(apple);

        Assert.False(T2ACooking.TryBeginUse(player, apple));
    }
}
