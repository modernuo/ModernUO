using System;
using Server;
using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;
using Server.Random;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class FishingTests
{
    // Open sea with no land within 30 tiles: deep water once the client's map is loaded.
    private static readonly Point3D _deep = new(1840, 1760, -5);

    [SkippableTheory]
    [InlineData(1840, 1760, true)]  // land 115 tiles away
    [InlineData(1660, 1760, true)]  // land 14 tiles away
    [InlineData(1659, 1760, false)] // land 13 tiles away
    [InlineData(2188, 1000, true)]  // land 23 tiles away, on a diagonal
    [InlineData(1718, 1913, false)] // land 8 tiles away, between the diagonals
    [InlineData(13, 200, true)]     // the edge of the map 14 tiles away
    [InlineData(12, 200, false)]    // the edge of the map 13 tiles away
    public void FullValidation_NeedsLandAndTheMapEdgeAtLeast14TilesAway(int x, int y, bool deep)
    {
        TileDataRequirement.SkipIfMissing();

        Assert.Equal(deep, SpecialFishingNet.FullValidation(Map.Felucca, x, y));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(2.5, false)]
    [InlineData(4.5, true)]
    public void Give_SerpentGoesBackAfterFiveMinutesUnlessHitInTheLastTwo(double? hitAtMinutes, bool stays)
    {
        Core._tickCount = 0;
        Timer.Init(0);

        var previousNow = Core._now;
        var previousHitsRate = Mobile.DefaultHitsRate;
        var fisher = new PlayerMobile();
        BaseCreature serpent = null;

        try
        {
            // The rate RegenRates sets before AOS, so the hit is not healed before the serpent's five minutes are up.
            Mobile.DefaultHitsRate = TimeSpan.FromSeconds(11.0);
            fisher.MoveToWorld(_deep, Map.Felucca);

            var bottle = new MessageInABottle(Map.Felucca);
            Fishing.System.Give(fisher, bottle, false);

            serpent = Assert.IsAssignableFrom<BaseCreature>(bottle.RootParent);
            serpent.AIObject?.AITimer.Stop();
            serpent.Combatant = null;

            var elapsed = TimeSpan.Zero;

            if (hitAtMinutes is { } minutes)
            {
                elapsed = TimeSpan.FromMinutes(minutes);
                Advance(elapsed);
                serpent.Damage(20, fisher);
                Assert.Same(fisher, serpent.FindMostRecentDamager(false));
            }

            Advance(TimeSpan.FromMinutes(4.9) - elapsed);
            Assert.False(serpent.Deleted);

            Advance(TimeSpan.FromMinutes(0.2));
            Assert.Equal(!stays, serpent.Deleted);
            Assert.Equal(!stays, bottle.Deleted);
        }
        finally
        {
            serpent?.Delete();
            fisher.Delete();
            Mobile.DefaultHitsRate = previousHitsRate;
            Core._now = previousNow;
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
            fisher.MoveToWorld(_deep, Map.Felucca);

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

    private static void Advance(TimeSpan time)
    {
        Core._now += time;
        Core._tickCount += (long)time.TotalMilliseconds;
        Timer.Slice(Core.TickCount);
    }

    // Map.Internal skips FindLocation's search; the wreck is then where the fisher stands.
    private static SOS AddSOS(Mobile fisher)
    {
        var sos = new SOS(Map.Internal, 1) { TargetMap = fisher.Map, TargetLocation = fisher.Location };
        fisher.Backpack.DropItem(sos);
        return sos;
    }
}
