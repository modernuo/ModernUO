using Server;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class CraftFindTests
{
    private static readonly int[] _campfire = [0xDE3, 0xDE3];

    [Fact]
    public void PlacedStationFarBelowThePlayerIsNotFound()
    {
        var from = new PlayerMobile { Player = true };
        var near = new Item(0xDE3) { Movable = false };
        var far = new Item(0xDE3) { Movable = false };
        from.MoveToWorld(new Point3D(1000, 1000, 50), Map.Felucca);
        far.MoveToWorld(new Point3D(1001, 1000, 20), Map.Felucca);

        try
        {
            Assert.False(CraftItem.Find(from, _campfire));

            near.MoveToWorld(new Point3D(1001, 1000, 50), Map.Felucca);
            Assert.True(CraftItem.Find(from, _campfire));
        }
        finally
        {
            from.Delete();
            near.Delete();
            far.Delete();
        }
    }
}
