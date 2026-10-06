using System;
using Server;
using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;
using Server.Random;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class FishingTests
{
    private const int Casts = 100_000;

    // On land in Britain, so a cast there never counts as deep water.
    private static readonly Point3D _shallow = new(1434, 1699, 0);

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

    // Within five standard deviations of the expected rate; a rate of zero must never happen.
    private static void AssertRate(double expected, int count)
    {
        var tolerance = 5 * Math.Sqrt(expected * (1 - expected) / Casts);
        Assert.InRange((double)count / Casts, expected - tolerance, expected + tolerance);
    }
}
