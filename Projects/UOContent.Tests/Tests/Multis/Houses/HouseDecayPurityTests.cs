using System;
using Server;
using Server.Mobiles;
using Server.Multis;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HouseDecayPurityTests
{
    [Fact]
    public void ReadingDecayLevelDoesNotStampLastRefreshed()
    {
        var decayEnabled = BaseHouse.DecayEnabled;
        BaseHouse.DecayEnabled = false; // every house is Ageless, the branch that used to restamp on read

        var owner = new PlayerMobile(World.NewMobile);
        owner.DefaultMobileInit();
        owner.MoveToWorld(new Point3D(1400, 1400, 0), Map.Felucca);

        var house = new SmallOldHouse(owner, 0x64);

        try
        {
            house.MoveToWorld(new Point3D(1400, 1400, 0), Map.Felucca);

            var placed = Core.Now - TimeSpan.FromDays(2);
            house.LastRefreshed = placed;

            Assert.False(house.CanDecay);
            Assert.Equal(DecayLevel.Ageless, house.DecayLevel);
            Assert.Equal(placed, house.LastRefreshed);
        }
        finally
        {
            house.Delete();
            owner.Delete();
            BaseHouse.DecayEnabled = decayEnabled;
        }
    }
}
