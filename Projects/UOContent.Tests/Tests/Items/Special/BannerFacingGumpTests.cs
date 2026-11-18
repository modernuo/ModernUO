using System;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BannerFacingGumpTests
{
    [Fact]
    public void CancelDoesNotPlaceTheBannerOrSpendTheDeed()
    {
        var owner = new PlayerMobile(World.NewMobile);
        owner.DefaultMobileInit();
        owner.MoveToWorld(new Point3D(1400, 1400, 0), Map.Felucca);
        var house = new SmallOldHouse(owner, 0x64);
        var deed = new BannerDeed();
        using var state = PacketTestUtilities.CreateTestNetState();
        state.Mobile = owner;

        try
        {
            var gumpType = typeof(BannerDeed).GetNestedType("InternalTarget", System.Reflection.BindingFlags.NonPublic)!
                .GetNestedType("FacingGump", System.Reflection.BindingFlags.NonPublic)!;
            var gump = (BaseGump)Activator.CreateInstance(
                gumpType, deed, 0x15AA, new Point3D(1400, 1400, 0), house
            )!;

            gump.OnResponse(state, new RelayInfo(0, [], [], [], []));

            Assert.False(deed.Deleted);
            Assert.Empty(house.Addons);
        }
        finally
        {
            state.Mobile = null;
            deed.Delete();
            house.Delete();
            owner.Delete();
        }
    }
}
