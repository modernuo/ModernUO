using Server;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Multis.Boats;
using Server.Systems.FeatureFlags;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BoatDeedPlacementTests
{
    private class FlagDeed : BaseBoatDeed
    {
        public bool BoatRequested;

        public FlagDeed() : base(0x0, Point3D.Zero)
        {
        }

        public override BaseBoat Boat
        {
            get
            {
                BoatRequested = true;
                return null;
            }
        }
    }

    [Fact]
    public void OnPlacement_StopsWhenBoatPlacementIsDisabled()
    {
        var saved = ContentFeatureFlags.BoatPlacement;
        var from = new PlayerMobile { Player = true };
        from.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        var deed = new FlagDeed();
        from.AddItem(new Backpack());
        from.Backpack.AddItem(deed);

        try
        {
            ContentFeatureFlags.BoatPlacement = false;
            deed.OnPlacement(from, new Point3D(1000, 1000, 0));
            Assert.False(deed.BoatRequested);
        }
        finally
        {
            ContentFeatureFlags.BoatPlacement = saved;
            deed.Delete();
            from.Backpack?.Delete();
            from.Delete();
        }
    }
}
