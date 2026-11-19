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

            // Roll 0 passes the quiver's lower ammo cost check, and would also pass the miss recovery chance
            using (new PredictableRandom(0))
            {
                Assert.True(bow.OnFired(player, target));
                bow.OnMiss(player, target);
            }

            player.Warmode = false;
            // Far past AmmoRecovery's recovery interval, so any banked ammo would have been gathered by now
            Timer.Slice(Core.TickCount + 60000);

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
