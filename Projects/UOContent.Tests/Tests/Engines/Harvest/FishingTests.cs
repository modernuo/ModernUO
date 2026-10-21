using System;
using Server;
using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;
using Server.Random;
using Server.Tests;
using Server.Tests.Terrain;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class FishingTests
{
    [Theory]
    [InlineData(32, 32, true)]  // the middle of the sea: land 32 tiles away
    [InlineData(13, 32, true)]  // land 14 tiles west
    [InlineData(12, 32, false)] // land 13 tiles west
    [InlineData(50, 32, true)]  // land 14 tiles east
    [InlineData(51, 32, false)] // land 13 tiles east
    [InlineData(13, 13, true)]  // land 14 tiles away on the diagonal
    [InlineData(13, 12, false)] // land 13 tiles north
    public void FullValidation_NeedsLandAtLeast14TilesAway(int dx, int dy, bool deep)
    {
        var sea = Prefabs.DeepSea;
        sea.AssertPresent();

        Assert.Equal(deep, SpecialFishingNet.FullValidation(Map.Felucca, sea.Bounds.X + dx, sea.Bounds.Y + dy));
    }

    [Theory]
    [InlineData(13, true)]  // the edge of the map 14 tiles away
    [InlineData(12, false)] // the edge of the map 13 tiles away
    public void FullValidation_TreatsTheMapEdgeAsLand(int x, bool deep)
    {
        var sea = Prefabs.MapEdgeSea;
        sea.AssertPresent();

        Assert.Equal(deep, SpecialFishingNet.FullValidation(Map.Felucca, x, sea.Anchor.Y));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(2.5, false)]
    [InlineData(4.5, true)]
    public void Give_SerpentGoesBackAfterFiveMinutesUnlessHitInTheLastTwo(double? hitAtMinutes, bool stays)
    {
        using var clock = new TimerWheelScope();

        var previousHitsRate = Mobile.DefaultHitsRate;
        var fisher = new PlayerMobile();
        BaseCreature serpent = null;

        try
        {
            // The rate RegenRates sets before AOS, so the hit is not healed before the serpent's five minutes are up.
            Mobile.DefaultHitsRate = TimeSpan.FromSeconds(11.0);
            Prefabs.DeepSea.AssertPresent();
            fisher.MoveToWorld(Prefabs.DeepSea.Anchor, Map.Felucca);

            var bottle = new MessageInABottle(Map.Felucca);
            Fishing.System.Give(fisher, bottle, false);

            serpent = Assert.IsAssignableFrom<BaseCreature>(bottle.RootParent);
            serpent.AIObject?.AITimer.Stop();
            serpent.Combatant = null;

            var elapsed = TimeSpan.Zero;

            if (hitAtMinutes is { } minutes)
            {
                elapsed = TimeSpan.FromMinutes(minutes);
                TimerWheelScope.Advance(elapsed);
                serpent.Damage(20, fisher);
                Assert.Same(fisher, serpent.FindMostRecentDamager(false));
            }

            TimerWheelScope.Advance(TimeSpan.FromMinutes(4.9) - elapsed);
            Assert.False(serpent.Deleted);

            TimerWheelScope.Advance(TimeSpan.FromMinutes(0.2));
            Assert.Equal(!stays, serpent.Deleted);
            Assert.Equal(!stays, bottle.Deleted);
        }
        finally
        {
            serpent?.Delete();
            fisher.Delete();
            Mobile.DefaultHitsRate = previousHitsRate;
        }
    }

    [Fact]
    public void Construct_SOSChestCatchConsumesTheSOS()
    {
        var previousExpansion = Core.Expansion;
        var previousGenerator = BuiltInRng.Generator;
        var fisher = new PlayerMobile();

        try
        {
            Core.Expansion = Expansion.UOR;
            BuiltInRng.Generator = new System.Random(1);
            fisher.Skills.Fishing.Base = 65.0;
            fisher.AddItem(new Backpack());
            Prefabs.DeepSea.AssertPresent();
            fisher.MoveToWorld(Prefabs.DeepSea.Anchor, Map.Felucca);

            var sos = AddSOS(fisher);

            for (var i = 0; i < 200; i++)
            {
                var item = Fishing.System.Construct(typeof(Fish), fisher);
                var isChest = item is WoodenChest or MetalGoldenChest;
                item.Delete();

                if (isChest)
                {
                    Assert.True(sos.Deleted);
                    return;
                }

                Assert.False(sos.Deleted);
            }

            Assert.Fail("No SOS chest surfaced in 200 casts");
        }
        finally
        {
            fisher.Delete();
            BuiltInRng.Generator = previousGenerator;
            Core.Expansion = previousExpansion;
        }
    }

    // Map.Internal skips FindLocation's search; the wreck is then where the fisher stands.
    private static SOS AddSOS(Mobile fisher)
    {
        var sos = new SOS(Map.Internal, 1) { TargetMap = fisher.Map, TargetLocation = fisher.Location };
        fisher.Backpack.DropItem(sos);
        return sos;
    }
}
