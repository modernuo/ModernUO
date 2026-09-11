using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Regions;
using Xunit;

namespace UOContent.Tests.Mobiles;

[Collection("Sequential UOContent Tests")]
public class MountRegionTests
{
    public static IEnumerable<object[]> MountCases()
    {
        foreach (var era in Enum.GetValues<Expansion>())
        {
            foreach (var allowed in new[] { false, true })
            {
                yield return new object[] { era, allowed, false };
                yield return new object[] { era, allowed, true };
            }
        }
    }

    [Theory]
    [MemberData(nameof(MountCases))]
    public void Mounting_RespectsActiveRegion(Expansion era, bool allowed, bool ethereal)
    {
        var previous = Core.Expansion;
        var region = new MountTestRegion(allowed);
        PlayerMobile player = null;
        TestHorse horse = null;
        EtherealHorse statue = null;
        try
        {
            Core.Expansion = era;
            region.Register();
            player = new PlayerMobile(World.NewMobile);
            player.DefaultMobileInit();
            player.Race = Race.Human;
            player.AddItem(new Backpack());
            player.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
            Assert.Same(region, player.Region);

            if (ethereal)
            {
                statue = new EtherealHorse { IsRewardItem = false };
                player.Backpack.DropItem(statue);
                Assert.Same(player.Backpack, statue.Parent);
                Assert.Equal(allowed, statue.Validate(player));
            }
            else
            {
                horse = new TestHorse();
                horse.MoveToWorld(player.Location, player.Map);
                horse.SetControlMaster(player);
                horse.OnDoubleClick(player);
                Assert.Equal(allowed, horse.Rider == player);
                Assert.Equal(allowed, player.Mounted);
            }
        }
        finally
        {
            horse?.Delete();
            statue?.Delete();
            player?.Delete();
            region.Unregister();
            Core.Expansion = previous;
        }
    }

    private class MountTestRegion : BaseRegion
    {
        private readonly bool _allowed;

        public MountTestRegion(bool allowed)
            : base("MountRegionTest", Map.Felucca, 100,
                new Rectangle3D(990, 990, -128, 20, 20, 256))
        {
            _allowed = allowed;
        }

        public override bool MountsAllowed => _allowed;
    }

    // The test fixture does not configure NPCSpeeds.
    private class TestHorse : Horse
    {
        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.2;
            passiveSpeed = 0.4;
        }
    }
}
