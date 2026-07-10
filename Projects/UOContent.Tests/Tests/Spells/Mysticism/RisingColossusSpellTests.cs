using System;
using System.Collections.Generic;
using System.Reflection;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Regions;
using Server.Spells;
using Server.Spells.Mysticism;
using Server.Text;
using Xunit;

namespace Server.Tests.Spells.Mysticism;

[Collection("Sequential UOContent Tests")]
public class RisingColossusSpellTests
{
    static RisingColossusSpellTests()
    {
        NPCSpeeds.RegisterSpeed(new NPCSpeeds.SpeedClassEntry
        {
            Level = SpeedLevel.Medium,
            ActiveSpeed = 0.25,
            PassiveSpeed = 0.5,
            Types = [typeof(RisingColossus)]
        });
    }

    [Fact]
    public void SpellMetadata_MatchesRisingColossusSources()
    {
        var caster = NewCaster();
        var spell = new RisingColossusSpell(caster);

        Assert.Equal("Rising Colossus", spell.Name);
        Assert.Equal("Kal Vas Xen Corp Ylem", spell.Mantra);
        Assert.Equal(SpellCircle.Eighth, spell.Circle);
        Assert.Equal(TimeSpan.FromSeconds(2.25), spell.CastDelayBase);
        Assert.Equal(50, spell.GetMana());
        Assert.Equal(83.0, spell.RequiredSkill);
        Assert.Equal(SkillName.Mysticism, spell.CastSkill);
        Assert.Equal(
            [Reagent.DaemonBone, Reagent.DragonsBlood, Reagent.FertileDirt, Reagent.Nightshade],
            spell.Reagents
        );

        caster.Delete();
    }

    [Fact]
    public void MysticSpellbookAndScroll_UseRisingColossusSlot()
    {
        var book = new MysticSpellbook(1UL << (692 - 677));
        var scroll = new RisingColossusScroll();

        Assert.Equal(677, book.BookOffset);
        Assert.Equal(16, book.BookCount);
        Assert.True(book.HasSpell(692));
        Assert.Equal(692, GetSpellScrollId(scroll));

        book.Delete();
        scroll.Delete();
    }

    [Fact]
    public void RegisterMysticism_PreSaDoesNotExposeRisingColossus()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.ML;
            Initializer.Configure();

            Assert.Null(SpellRegistry.NewSpell(692, caster, null));
            Assert.Equal(-1, SpellRegistry.GetRegistryNumber(typeof(RisingColossusSpell)));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            ResetSpellRegistry();
            Initializer.Configure();
            caster.Delete();
        }
    }

    [Fact]
    public void RegisterMysticism_SaExposesRisingColossusAtSpellId692()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.SA;
            Initializer.Configure();

            Assert.Same(typeof(RisingColossusSpell), SpellRegistry.Types[692]);
            Assert.Equal(692, SpellRegistry.GetRegistryNumber(typeof(RisingColossusSpell)));
            Assert.IsType<RisingColossusSpell>(SpellRegistry.NewSpell(692, caster, null));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            ResetSpellRegistry();
            Initializer.Configure();
            caster.Delete();
        }
    }

    [Theory]
    [InlineData(83.0, 0.0, 27.6666666667)]
    [InlineData(120.0, 0.0, 40.0)]
    [InlineData(120.0, 120.0, 60.0)]
    public void Duration_UsesMysticismAndHigherSupportSkill(double mysticism, double supportSkill, double expectedSeconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), RisingColossusSpell.GetDuration(mysticism, supportSkill));
    }

    [Fact]
    public void Summon_UsesFiveFollowerSlotsAndSkillScaledPower()
    {
        var caster = NewCaster(mysticism: 120.0, focus: 120.0);
        var summon = new RisingColossus(caster, 120.0, 120.0);

        Assert.Equal(5, summon.ControlSlots);
        Assert.Equal(780, summon.Str);
        Assert.Equal(210, summon.Dex);
        Assert.Equal(230, summon.Int);
        Assert.Equal(469, summon.HitsMax);
        Assert.Equal(20, summon.DamageMin);
        Assert.Equal(24, summon.DamageMax);
        Assert.Equal(120.0, summon.Skills.MagicResist.Value);
        Assert.Equal(120.0, summon.Skills.Tactics.Value);
        Assert.Equal(120.0, summon.Skills.Wrestling.Value);
        Assert.Equal(98.5, summon.DispelDifficulty);
        Assert.Equal(45.0, summon.DispelFocus);
        Assert.True(summon.BleedImmune);
        Assert.Equal(Poison.Lethal, summon.PoisonImmune);
        Assert.True(summon.AlwaysMurderer);

        summon.Delete();
        caster.Delete();
    }

    [Fact]
    public void Serialization_PreservesDispelDifficulty()
    {
        var caster = NewCaster();
        var original = new RisingColossus(caster, 120.0, 120.0);
        var deserialized = new RisingColossus(caster, 83.0, 0.0);

        try
        {
            var writer = new BufferWriter(true);
            original.Serialize(writer);
            var buffer = new byte[writer.Position];
            writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

            var reader = new BufferReader(buffer);
            deserialized.Deserialize(reader);

            Assert.Equal(buffer.Length, reader.Position);
            Assert.Equal(original.DispelDifficulty, deserialized.DispelDifficulty);
        }
        finally
        {
            original.Delete();
            deserialized.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void CheckCast_RejectsInsufficientSkillManaAndFollowerCapacity()
    {
        var lowSkillCaster = NewCaster(mysticism: 82.9);
        var lowSkillSpell = new RisingColossusSpell(lowSkillCaster);
        Assert.False(lowSkillSpell.CheckCast());
        lowSkillCaster.Delete();

        var lowManaCaster = NewCaster();
        lowManaCaster.Mana = 0;
        var lowManaSpell = new RisingColossusSpell(lowManaCaster);
        Assert.False(lowManaSpell.CheckCast());
        lowManaCaster.Delete();

        var fullFollowerCaster = NewCaster();
        fullFollowerCaster.Followers = fullFollowerCaster.FollowersMax - 4;
        var fullFollowerSpell = new RisingColossusSpell(fullFollowerCaster);
        Assert.False(fullFollowerSpell.CheckCast());
        fullFollowerCaster.Delete();
    }

    [Fact]
    public void TargetSelection_RanksIntelligentTargetsByProximity()
    {
        var caster = NewCaster();
        var summon = new RisingColossus(caster, 120.0, 120.0);
        var nearby = NewMobile(new Point3D(6201, 500, 0));
        var intelligent = NewMobile(new Point3D(6204, 500, 0));
        intelligent.Int = 120;
        intelligent.Skills.Magery.Base = 120.0;

        Assert.True(summon.GetFightModeRanking(intelligent, FightMode.Closest, false) >
                    summon.GetFightModeRanking(nearby, FightMode.Closest, false));

        intelligent.Delete();
        nearby.Delete();
        summon.Delete();
        caster.Delete();
    }

    [Fact]
    public void TargetLocation_RejectsHouseLocationsForTargetAndCaster()
    {
        var caster = NewCaster();
        var houseLocation = FindSpawnLocation();
        var outside = new Point3D(houseLocation.X + 10, houseLocation.Y + 10, houseLocation.Z);
        var house = new TestHouse(caster);
        house.MoveToWorld(houseLocation, Map.Felucca);

        try
        {
            Assert.True(RisingColossusSpell.IsHouseLocation(caster, houseLocation, Map.Felucca));

            caster.MoveToWorld(houseLocation, Map.Felucca);
            Assert.True(RisingColossusSpell.IsHouseLocation(caster, outside, Map.Felucca));
        }
        finally
        {
            house.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void SuccessfulTarget_ConsumesManaAndCanonicalReagentsAndCreatesSummon()
    {
        var previousExpansion = Core.Expansion;
        Core.Expansion = Expansion.SA;

        var caster = NewCaster();
        AddReagents(caster);
        var summonLocation = FindSpawnLocation();
        caster.MoveToWorld(new Point3D(summonLocation.X - 1, summonLocation.Y, summonLocation.Z), Map.Felucca);
        var spell = new TestRisingColossusSpell(caster);
        caster.Spell = spell;
        spell.State = SpellState.Sequencing;

        try
        {
            Assert.True(SpellHelper.CheckTown(summonLocation, caster));
            spell.Target(summonLocation);

            var summon = FindSummon(caster);
            Assert.Equal(50, caster.Mana);
            Assert.Equal(5, caster.Followers);
            Assert.NotNull(summon);
            Assert.Equal(5, caster.Followers);
            Assert.Equal(50, caster.Mana);
            Assert.Equal(9, caster.Backpack.FindItemByType<DaemonBone>().Amount);
            Assert.Equal(9, caster.Backpack.FindItemByType<DragonsBlood>().Amount);
            Assert.Equal(9, caster.Backpack.FindItemByType<FertileDirt>().Amount);
            Assert.Equal(9, caster.Backpack.FindItemByType<Nightshade>().Amount);

            summon.Delete();
            Assert.Equal(0, caster.Followers);
        }
        finally
        {
            spell.FinishSequence();
            caster.Delete();
            Core.Expansion = previousExpansion;
        }
    }

    [Fact]
    public void TargetLocation_RejectsCustomNoSummonRegion()
    {
        var previousExpansion = Core.Expansion;
        Core.Expansion = Expansion.SA;

        var caster = NewCaster();
        AddReagents(caster);
        var summonLocation = FindSpawnLocation();
        caster.MoveToWorld(new Point3D(summonLocation.X - 1, summonLocation.Y, summonLocation.Z), Map.Felucca);
        var region = new NoSummonRegion(summonLocation);
        region.Register();
        var spell = new TestRisingColossusSpell(caster);
        caster.Spell = spell;
        spell.State = SpellState.Sequencing;

        try
        {
            Assert.True(RisingColossusSpell.IsNoSummonRegion(summonLocation, Map.Felucca));
            spell.Target(summonLocation);

            Assert.Equal(100, caster.Mana);
            Assert.Equal(0, caster.Followers);
            Assert.Equal(10, caster.Backpack.FindItemByType<DaemonBone>().Amount);
            Assert.Null(FindSummon(caster));
        }
        finally
        {
            region.Unregister();
            spell.FinishSequence();
            caster.Delete();
            Core.Expansion = previousExpansion;
        }
    }

    [Fact]
    public void BlockedTarget_DoesNotConsumeResourcesOrCreateSummon()
    {
        var previousExpansion = Core.Expansion;
        Core.Expansion = Expansion.SA;

        var caster = NewCaster();
        AddReagents(caster);
        var summonLocation = FindSpawnLocation();
        var blockedLocation = FindBlockedLocation();
        caster.MoveToWorld(new Point3D(summonLocation.X - 1, summonLocation.Y, summonLocation.Z), Map.Felucca);
        var spell = new TestRisingColossusSpell(caster);
        caster.Spell = spell;
        spell.State = SpellState.Sequencing;

        try
        {
            spell.Target(blockedLocation);

            Assert.Equal(100, caster.Mana);
            Assert.Equal(0, caster.Followers);
            Assert.Equal(10, caster.Backpack.FindItemByType<DaemonBone>().Amount);
            Assert.Null(FindSummon(caster));
        }
        finally
        {
            spell.FinishSequence();
            caster.Delete();
            Core.Expansion = previousExpansion;
        }
    }

    private static RisingColossus FindSummon(Mobile caster)
    {
        foreach (var mobile in caster.Map.GetMobilesInRange(caster.Location, 2))
        {
            if (mobile is RisingColossus summon && summon.SummonMaster == caster)
            {
                return summon;
            }
        }

        return null;
    }

    private static PlayerMobile NewCaster(double mysticism = 120.0, double focus = 120.0, double imbuing = 0.0)
    {
        var caster = new PlayerMobile(World.NewMobile);
        caster.DefaultMobileInit();
        caster.Player = true;
        caster.InitStats(100, 100, 100);
        caster.Mana = caster.ManaMax;
        caster.AddItem(new Backpack());
        caster.Skills.Mysticism.Base = mysticism;
        caster.Skills.Focus.Base = focus;
        caster.Skills.Imbuing.Base = imbuing;
        return caster;
    }

    private static Mobile NewMobile(Point3D location)
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.InitStats(100, 100, 100);
        mobile.MoveToWorld(location, Map.Felucca);
        return mobile;
    }

    private static Point3D FindSpawnLocation()
    {
        for (var x = 6100; x < 6200; ++x)
        {
            for (var y = 400; y < 600; ++y)
            {
                var z = Map.Felucca.GetAverageZ(x, y);
                if (Map.Felucca.CanSpawnMobile(x, y, z))
                {
                    return new Point3D(x, y, z);
                }
            }
        }

        throw new InvalidOperationException("No valid Felucca summon location was found for the test fixture.");
    }

    private static Point3D FindBlockedLocation()
    {
        for (var x = 6200; x < 6300; ++x)
        {
            for (var y = 400; y < 600; ++y)
            {
                var z = Map.Felucca.GetAverageZ(x, y);
                if (!Map.Felucca.CanSpawnMobile(x, y, z))
                {
                    return new Point3D(x, y, z);
                }
            }
        }

        throw new InvalidOperationException("No blocked Felucca summon location was found for the test fixture.");
    }

    private static void AddReagents(Mobile caster)
    {
        caster.AddToBackpack(new DaemonBone(10));
        caster.AddToBackpack(new DragonsBlood(10));
        caster.AddToBackpack(new FertileDirt(10));
        caster.AddToBackpack(new Nightshade(10));
    }

    private static int GetSpellScrollId(SpellScroll scroll)
    {
        var field = typeof(SpellScroll).GetField("_spellID", BindingFlags.Instance | BindingFlags.NonPublic);
        return (int)field!.GetValue(scroll)!;
    }

    private static void ResetSpellRegistry()
    {
        var types = (Type[])typeof(SpellRegistry)
            .GetField("m_Types", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
        Array.Clear(types);

        var idsFromTypes = (Dictionary<Type, int>)typeof(SpellRegistry)
            .GetField("m_IDsFromTypes", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
        idsFromTypes.Clear();

        typeof(SpellRegistry)
            .GetField("m_Count", BindingFlags.Static | BindingFlags.NonPublic)!
            .SetValue(null, 0);

        SpellRegistry.SpecialMoves.Clear();
    }

    private sealed class NoSummonRegion : BaseRegion
    {
        public NoSummonRegion(Point3D location) :
            base("Rising Colossus NoSummon Test", Map.Felucca, 1000,
                new Rectangle3D(location.X, location.Y, -128, 1, 1, 256))
        {
        }

        public override bool AllowSpawn() => false;
    }

    private sealed class TestHouse : BaseHouse
    {
        public TestHouse(Mobile owner) : base(0, owner, 0, 0)
        {
        }

        public override Rectangle2D[] Area => [new Rectangle2D(-2, -2, 5, 5)];
        public override Point3D BaseBanLocation => Point3D.Zero;
    }

    private sealed class TestRisingColossusSpell : RisingColossusSpell
    {
        public TestRisingColossusSpell(Mobile caster) : base(caster)
        {
        }

        public override bool CheckFizzle() => true;
    }
}