using Server.Items;
using Server.Mobiles;
using Xunit;

namespace Server.Tests.Items.Weapons.Abilities;

[Collection("Sequential UOContent Tests")]
public class QuiverAmmoRecoveryTests
{
    [Fact]
    public void ShotThatSpentNoAmmo_BanksNothingOnMiss()
    {
        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        player.Player = true;
        var target = new Mobile(World.NewMobile);
        target.DefaultMobileInit();

        try
        {
            var pack = new Backpack { Layer = Layer.Backpack };
            player.AddItem(pack);
            pack.DropItem(new Arrow(10));
            player.AddItem(new QuiverOfInfinity());
            var bow = new Bow { Layer = Layer.TwoHanded };
            player.AddItem(bow);

            using (new PredictableRandom(0)) // lower ammo cost saves the arrow; the miss roll is under 40%
            {
                Assert.True(bow.OnFired(player, target));
                bow.OnMiss(player, target);
            }

            player.Warmode = false;
            Timer.Slice(Core.TickCount + 11000);

            Assert.Equal(10, pack.GetAmount(typeof(Arrow)));
        }
        finally
        {
            AmmoRecovery.Forget(player);
            player.Delete();
            target.Delete();
        }
    }
}
