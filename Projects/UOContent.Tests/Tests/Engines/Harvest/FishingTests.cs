using System;
using System.Collections.Generic;
using Server;
using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;
using Server.Random;
using Server.Targeting;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class FishingTests
{
    private const int Casts = 100_000;

    // On land in Britain, so a cast there never counts as deep water.
    private static readonly Point3D _shallow = new(1434, 1699, 0);

    // Open sea with no land within 30 tiles: deep water once the client's map is loaded.
    private static readonly Point3D _deep = new(1840, 1760, -5);

    private static readonly Type[] _magicFish =
        [typeof(PrizedFish), typeof(WondrousFish), typeof(TrulyRareFish), typeof(PeculiarFish)];

    private static readonly Type[] _footwear = [typeof(Boots), typeof(Shoes), typeof(Sandals), typeof(ThighBoots)];

    [Theory]
    [InlineData(Expansion.None, 50.0, 0.0, 0.0)]
    [InlineData(Expansion.T2A, 0.0, 0.0, 0.75)]
    [InlineData(Expansion.T2A, 50.0, 0.0, 0.3877)]
    [InlineData(Expansion.T2A, 100.0, 0.025, 0.0247)]
    [InlineData(Expansion.UOTD, 80.0, 0.02, 0.1669)]
    [InlineData(Expansion.LBR, 0.0, 0.0, 0.75)]
    [InlineData(Expansion.LBR, 50.0, 0.025, 0.4519)]
    [InlineData(Expansion.AOS, 100.0, 0.05, 0.095)]
    [InlineData(Expansion.SA, 50.0, 0.025, 0.4519)]
    [InlineData(Expansion.HS, 50.0, 0.0, 0.4635)]
    [InlineData(Expansion.HS, 120.0, 0.06, 0.047)]
    [InlineData(Expansion.EJ, 120.0, 0.06, 0.047)]
    public void MutateType_MagicFishAndFootwear_FollowTheEra(
        Expansion expansion, double skill, double magicFishRate, double footwearRate
    )
    {
        var previousExpansion = Core.Expansion;
        var previousGenerator = BuiltInRng.Generator;
        var fisher = new PlayerMobile(World.NewMobile);

        try
        {
            Core.Expansion = expansion;
            BuiltInRng.Generator = new System.Random(1);
            fisher.DefaultMobileInit();
            fisher.Skills.Fishing.Base = skill;

            Assert.Equal(skill, fisher.Skills.Fishing.Value);
            Assert.False(SpecialFishingNet.FullValidation(Map.Felucca, _shallow.X, _shallow.Y));

            var definition = Fishing.System.Definitions[0];
            var magicFish = 0;
            var footwear = 0;

            for (var i = 0; i < Casts; i++)
            {
                var type = Fishing.System.MutateType(typeof(Fish), fisher, null, definition, Map.Felucca, _shallow, null);

                if (Array.IndexOf(_magicFish, type) >= 0)
                {
                    magicFish++;
                }
                else if (Array.IndexOf(_footwear, type) >= 0)
                {
                    footwear++;
                }
            }

            AssertRate(magicFishRate, magicFish);
            AssertRate(footwearRate, footwear);
        }
        finally
        {
            fisher.Delete();
            BuiltInRng.Generator = previousGenerator;
            Core.Expansion = previousExpansion;
        }
    }

    [Fact]
    public void Leviathan_CarriesItsTreasureMapAQuarterOfTheTime()
    {
        var leviathan = new Leviathan();
        var serpent = new SeaSerpent();

        try
        {
            Assert.Equal(0.25, leviathan.TreasureMapChance);
            Assert.Equal(TreasureMap.LootChance, serpent.TreasureMapChance);
        }
        finally
        {
            leviathan.Delete();
            serpent.Delete();
        }
    }

    [Theory]
    [InlineData(Expansion.None, 0.0)]
    [InlineData(Expansion.T2A, 100.0)]
    [InlineData(Expansion.UOR, 50.0)]
    [InlineData(Expansion.LBR, 0.0)]
    [InlineData(Expansion.EJ, 120.0)]
    public void MutateType_NeverComesBackEmpty(Expansion expansion, double skill)
    {
        var previousExpansion = Core.Expansion;
        var previousGenerator = BuiltInRng.Generator;
        var fisher = new PlayerMobile(World.NewMobile);

        try
        {
            Core.Expansion = expansion;
            BuiltInRng.Generator = new System.Random(1);
            fisher.DefaultMobileInit();
            fisher.Skills.Fishing.Base = skill;

            var definition = Fishing.System.Definitions[0];

            for (var i = 0; i < Casts; i++)
            {
                var type = Fishing.System.MutateType(typeof(Fish), fisher, null, definition, Map.Felucca, _shallow, null);
                Assert.NotNull(type);
            }
        }
        finally
        {
            fisher.Delete();
            BuiltInRng.Generator = previousGenerator;
            Core.Expansion = previousExpansion;
        }
    }

    [SkippableTheory]
    [InlineData(Expansion.T2A, 100.0, 0.005, 0.004975)]
    [InlineData(Expansion.UOTD, 100.0, 0.00495, 0.004925)]
    [InlineData(Expansion.LBR, 95.0, 0.0062, 0.0)]
    [InlineData(Expansion.LBR, 100.0, 0.00825, 0.01309)]
    [InlineData(Expansion.HS, 120.0, 0.016335, 0.0257)]
    [InlineData(Expansion.EJ, 120.0, 0.016335, 0.0257)]
    public void MutateType_DeepWaterMapsAndBottles_FollowTheEra(
        Expansion expansion, double skill, double mapRate, double bottleRate
    )
    {
        TileDataRequirement.SkipIfMissing();
        Assert.True(SpecialFishingNet.FullValidation(Map.Felucca, _deep.X, _deep.Y));

        var previousExpansion = Core.Expansion;
        var previousGenerator = BuiltInRng.Generator;
        var fisher = new PlayerMobile(World.NewMobile);

        try
        {
            Core.Expansion = expansion;
            BuiltInRng.Generator = new System.Random(1);
            fisher.DefaultMobileInit();
            fisher.Skills.Fishing.Base = skill;

            var definition = Fishing.System.Definitions[0];
            var maps = 0;
            var bottles = 0;

            for (var i = 0; i < Casts; i++)
            {
                var type = Fishing.System.MutateType(typeof(Fish), fisher, null, definition, Map.Felucca, _deep, null);

                if (type == typeof(TreasureMap))
                {
                    maps++;
                }
                else if (type == typeof(MessageInABottle))
                {
                    bottles++;
                }
            }

            AssertRate(mapRate, maps);
            AssertRate(bottleRate, bottles);
        }
        finally
        {
            fisher.Delete();
            BuiltInRng.Generator = previousGenerator;
            Core.Expansion = previousExpansion;
        }
    }

    [SkippableTheory]
    [InlineData(Expansion.T2A, true, 0.0, 100.0)]
    [InlineData(Expansion.UOR, false, 0.0, 100.0)]
    [InlineData(Expansion.UOR, true, 65.0, 115.0)]
    [InlineData(Expansion.EJ, true, 65.0, 115.0)]
    public void MutateResource_DeepWaterIsHarderFromUOR(Expansion expansion, bool deep, double minSkill, double maxSkill)
    {
        if (deep)
        {
            TileDataRequirement.SkipIfMissing();
            Assert.True(SpecialFishingNet.FullValidation(Map.Felucca, _deep.X, _deep.Y));
        }

        var previousExpansion = Core.Expansion;
        var fisher = new PlayerMobile(World.NewMobile);

        try
        {
            Core.Expansion = expansion;
            fisher.DefaultMobileInit();

            var definition = Fishing.System.Definitions[0];
            var vein = definition.Veins[0];
            var resource = Fishing.System.MutateResource(
                fisher, null, definition, Map.Felucca, deep ? _deep : _shallow, vein, vein.PrimaryResource, null
            );

            Assert.Equal(minSkill, resource.MinSkill);
            Assert.Equal(maxSkill, resource.MaxSkill);
        }
        finally
        {
            fisher.Delete();
            Core.Expansion = previousExpansion;
        }
    }

    [Theory]
    [InlineData(Expansion.T2A, 0.0)]
    [InlineData(Expansion.UOTD, 0.0)]
    [InlineData(Expansion.LBR, 0.1)]
    [InlineData(Expansion.EJ, 0.1)]
    public void SpecialFishingNet_IsColoredOneTimeInTenFromLBR(Expansion expansion, double coloredRate)
    {
        var previousExpansion = Core.Expansion;
        var previousGenerator = BuiltInRng.Generator;

        try
        {
            Core.Expansion = expansion;
            BuiltInRng.Generator = new System.Random(1);

            var colored = 0;

            for (var i = 0; i < Casts; i++)
            {
                var net = new SpecialFishingNet();

                if (net.Hue != 0x8A0)
                {
                    colored++;
                }

                net.Delete();
            }

            AssertRate(coloredRate, colored);
        }
        finally
        {
            BuiltInRng.Generator = previousGenerator;
            Core.Expansion = previousExpansion;
        }
    }

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

    [SkippableFact]
    public void SOS_FindLocation_PicksDeepWater()
    {
        TileDataRequirement.SkipIfMissing();

        var previousGenerator = BuiltInRng.Generator;

        try
        {
            BuiltInRng.Generator = new System.Random(1);

            for (var i = 0; i < 100; i++)
            {
                var location = SOS.FindLocation(Map.Felucca);

                Assert.NotEqual(Point3D.Zero, location);
                Assert.True(SpecialFishingNet.FullValidation(Map.Felucca, location.X, location.Y));
            }
        }
        finally
        {
            BuiltInRng.Generator = previousGenerator;
        }
    }

    [Fact]
    public void Give_SurfacesASerpentThatAttacksAndIsADeepSeaSerpentOneTimeInTen()
    {
        const int serpents = 2_000;

        var previousGenerator = BuiltInRng.Generator;
        var fisher = new PlayerMobile();

        try
        {
            BuiltInRng.Generator = new System.Random(1);
            fisher.MoveToWorld(_deep, Map.Felucca);

            var deepSeaSerpents = 0;

            for (var i = 0; i < serpents; i++)
            {
                var bottle = new MessageInABottle(Map.Felucca);

                Assert.True(Fishing.System.Give(fisher, bottle, false));

                var serpent = Assert.IsAssignableFrom<BaseCreature>(bottle.RootParent);
                Assert.Same(fisher, serpent.Combatant);

                if (serpent is DeepSeaSerpent)
                {
                    deepSeaSerpents++;
                }

                serpent.Delete();
            }

            AssertRate(0.1, deepSeaSerpents, serpents);
        }
        finally
        {
            fisher.Delete();
            BuiltInRng.Generator = previousGenerator;
        }
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

    [SkippableTheory]
    [InlineData(Expansion.None, 100.0, true, 0.0)]
    [InlineData(Expansion.T2A, 79.9, true, 0.0)]
    [InlineData(Expansion.T2A, 80.0, true, 0.01)]
    [InlineData(Expansion.T2A, 100.0, false, 0.0)]
    [InlineData(Expansion.EJ, 120.0, true, 0.01)]
    public void OnHarvestFinished_SurfacesAnEmptySerpentOneCastInAHundred(
        Expansion expansion, double skill, bool deep, double serpentRate
    )
    {
        TileDataRequirement.SkipIfMissing();
        Assert.True(SpecialFishingNet.FullValidation(Map.Felucca, _deep.X, _deep.Y));

        var previousExpansion = Core.Expansion;
        var previousGenerator = BuiltInRng.Generator;
        var fisher = new PlayerMobile();

        try
        {
            Core.Expansion = expansion;
            BuiltInRng.Generator = new System.Random(1);
            fisher.Skills.Fishing.Base = skill;
            fisher.MoveToWorld(_deep, Map.Felucca);

            var definition = Fishing.System.Definitions[0];
            var target = new LandTarget(deep ? _deep : _shallow, Map.Felucca);
            var surfaced = new List<BaseCreature>();
            var serpents = 0;

            for (var i = 0; i < Casts; i++)
            {
                Fishing.System.OnHarvestFinished(fisher, null, definition, null, null, null, target);

                foreach (var serpent in Map.Felucca.GetMobilesInRange<BaseCreature>(fisher.Location, 10))
                {
                    surfaced.Add(serpent);
                }

                serpents += surfaced.Count;

                foreach (var serpent in surfaced)
                {
                    serpent.Delete();
                }

                surfaced.Clear();
            }

            AssertRate(serpentRate, serpents);
        }
        finally
        {
            fisher.Delete();
            BuiltInRng.Generator = previousGenerator;
            Core.Expansion = previousExpansion;
        }
    }

    [Theory]
    [InlineData(Expansion.T2A, 0.0)]
    [InlineData(Expansion.UOR, 65.0)]
    [InlineData(Expansion.EJ, 120.0)]
    public void Construct_BringsUpTheSOSChestOneCatchInFour(Expansion expansion, double skill)
    {
        const int casts = 2_000;

        var previousExpansion = Core.Expansion;
        var previousGenerator = BuiltInRng.Generator;
        var fisher = new PlayerMobile();

        try
        {
            Core.Expansion = expansion;
            BuiltInRng.Generator = new System.Random(1);
            fisher.Skills.Fishing.Base = skill;
            fisher.AddItem(new Backpack());
            fisher.MoveToWorld(_deep, Map.Felucca);

            var sos = AddSOS(fisher);
            var chests = 0;

            for (var i = 0; i < casts; i++)
            {
                var item = Fishing.System.Construct(typeof(Fish), fisher);

                if (item is WoodenChest or MetalGoldenChest)
                {
                    chests++;
                    Assert.True(sos.Deleted);
                    sos = AddSOS(fisher);
                }
                else
                {
                    Assert.True(Assert.IsAssignableFrom<IShipwreckedItem>(item).IsShipwreckedItem);
                }

                item.Delete();
            }

            AssertRate(0.25, chests, casts);
        }
        finally
        {
            fisher.Delete();
            BuiltInRng.Generator = previousGenerator;
            Core.Expansion = previousExpansion;
        }
    }

    [Theory]
    [InlineData(Expansion.T2A, 0.0, true)]
    [InlineData(Expansion.UOR, 64.9, false)]
    [InlineData(Expansion.UOR, 65.0, true)]
    [InlineData(Expansion.EJ, 64.9, false)]
    public void SOS_Needs65SkillFromUOR(Expansion expansion, double skill, bool recovers)
    {
        var previousExpansion = Core.Expansion;
        var definition = Fishing.System.Definitions[0];
        var fisher = new PlayerMobile();
        Item item = null;

        try
        {
            Core.Expansion = expansion;
            fisher.Skills.Fishing.Base = skill;
            fisher.AddItem(new Backpack());
            fisher.MoveToWorld(_deep, Map.Felucca);
            AddSOS(fisher);

            // With no fish left, only an SOS that works lets the cast go on.
            var bank = definition.GetBank(Map.Felucca, _deep.X, _deep.Y);
            bank.Consume(bank.Current, fisher);
            Assert.Equal(recovers, Fishing.System.CheckResources(fisher, null, definition, Map.Felucca, _deep, false));

            item = Fishing.System.Construct(typeof(Fish), fisher);
            Assert.Equal(recovers, item is not Fish);
        }
        finally
        {
            item?.Delete();
            fisher.Delete();
            definition.Banks.GetValueOrDefault(Map.Felucca)
                ?.Remove(new Point2D(_deep.X / definition.BankWidth, _deep.Y / definition.BankHeight));
            Core.Expansion = previousExpansion;
        }
    }

    [Theory]
    [InlineData(Expansion.None, false)]
    [InlineData(Expansion.T2A, true)]
    [InlineData(Expansion.AOS, true)]
    [InlineData(Expansion.ML, true)]
    public void OnHarvestStartedAndFinished_RevealAHiddenFishermanFromT2A(Expansion expansion, bool reveals)
    {
        Core._tickCount = 0;
        Timer.Init(0);

        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var fisher = new PlayerMobile();

        try
        {
            Core.Expansion = expansion;
            fisher.MoveToWorld(_deep, Map.Felucca);

            var definition = Fishing.System.Definitions[0];
            var target = new LandTarget(_deep, Map.Felucca);

            fisher.Hidden = true;
            Fishing.System.OnHarvestStarted(fisher, null, definition, target);
            Advance(TimeSpan.FromSeconds(2.0));
            Assert.Equal(!reveals, fisher.Hidden);

            fisher.Hidden = true;
            Fishing.System.OnHarvestFinished(fisher, null, definition, null, null, null, target);
            Assert.Equal(!reveals, fisher.Hidden);
        }
        finally
        {
            fisher.Delete();
            Core._now = previousNow;
            Core.Expansion = previousExpansion;
        }
    }

    // Within five standard deviations of the expected rate; a rate of zero must never happen.
    private static void AssertRate(double expected, int count)
    {
        var tolerance = 5 * Math.Sqrt(expected * (1 - expected) / Casts);
        Assert.InRange((double)count / Casts, expected - tolerance, expected + tolerance);
    }

    private static void AssertRate(double expected, int count, int trials)
    {
        var tolerance = 5 * Math.Sqrt(expected * (1 - expected) / trials);
        Assert.InRange((double)count / trials, expected - tolerance, expected + tolerance);
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
