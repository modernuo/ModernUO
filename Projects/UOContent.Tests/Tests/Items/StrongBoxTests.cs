using Server;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class StrongBoxTests
{
    private static PlayerMobile NewPlayer()
    {
        var m = new PlayerMobile(World.NewMobile);
        m.DefaultMobileInit();
        m.MoveToWorld(new Point3D(1400, 1400, 0), Map.Felucca);
        return m;
    }

    [Fact]
    public void OrphanedStrongboxIsChoppableOnlyByHouseOwner()
    {
        var houseOwner = NewPlayer();
        var boxOwner = NewPlayer();
        var thief = NewPlayer();
        var house = new SmallOldHouse(houseOwner, 0x64);
        var box = new StrongBox(boxOwner, house) { Movable = false };

        try
        {
            house.MoveToWorld(new Point3D(1400, 1400, 0), Map.Felucca);
            box.MoveToWorld(new Point3D(1400, 1400, 0), Map.Felucca);
            boxOwner.Delete();

            Assert.False(box.IsAccessibleTo(thief));
            box.OnChop(thief);
            Assert.False(box.Deleted);
            box.OnChop(houseOwner);
            Assert.True(box.Deleted);
        }
        finally
        {
            box.Delete();
            house.Delete();
            thief.Delete();
            houseOwner.Delete();
            boxOwner.Delete();
        }
    }
}
