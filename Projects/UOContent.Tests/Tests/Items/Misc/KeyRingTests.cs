using Server;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class KeyRingTests
{
    [Fact]
    public void KeyRing_CannotUnlockBeyondTheKeyRange()
    {
        var player = new PlayerMobile { Player = true, Body = 0x190 };
        player.MoveToWorld(new Point3D(1300, 1000, 0), Map.Felucca);
        player.AddItem(new Backpack());
        var chest = new WoodenChest { Locked = true, KeyValue = 77 };
        chest.MoveToWorld(new Point3D(1305, 1000, 0), Map.Felucca);
        var ring = new KeyRing();
        player.Backpack.AddItem(ring);
        ring.Add(new Key(77));

        try
        {
            ring.OnDoubleClick(player);
            player.Target.Invoke(player, chest);

            Assert.True(chest.Locked);
        }
        finally
        {
            player.Target = null;
            chest.Delete();
            player.Delete();
        }
    }
}
