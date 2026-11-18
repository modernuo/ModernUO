using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class CowMilkTests
{
    [Fact]
    public void TryMilkRefusesFromOutOfRange()
    {
        var player = new PlayerMobile { Player = true };
        var cow = new Cow();
        player.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        cow.MoveToWorld(new Point3D(1020, 1000, 0), Map.Felucca);

        try
        {
            var milk = cow.Milk;

            Assert.False(cow.TryMilk(player));
            Assert.Equal(milk, cow.Milk);
        }
        finally
        {
            cow.Delete();
            player.Delete();
        }
    }
}
