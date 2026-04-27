using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class ThrowingTests
{
    // Exposes protected ModifyHitChance so the math can be verified directly.
    private class TestThrown : BaseThrown
    {
        public TestThrown() : base(0x8FF) { }

        public override int MinThrowRange => 4;

        public double TestModifyHitChance(Mobile attacker, Mobile defender, double chance) =>
            ModifyHitChance(attacker, defender, chance);
    }

    // Weapon properties

    [Theory]
    [InlineData(typeof(Boomerang))]
    [InlineData(typeof(Cyclone))]
    [InlineData(typeof(SoulGlaive))]
    public void ThrowingWeapon_AccuracySkill_IsThrowingSkill(Type weaponType)
    {
        var weapon = (BaseThrown)Activator.CreateInstance(weaponType);
        try
        {
            Assert.Equal(SkillName.Throwing, weapon.AccuracySkill);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Theory]
    [InlineData(typeof(Boomerang))]
    [InlineData(typeof(Cyclone))]
    [InlineData(typeof(SoulGlaive))]
    public void ThrowingWeapon_DefSkill_IsThrowingSkill(Type weaponType)
    {
        var weapon = (BaseThrown)Activator.CreateInstance(weaponType);
        try
        {
            Assert.Equal(SkillName.Throwing, weapon.DefSkill);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Theory]
    [InlineData(typeof(Boomerang), 4, 7)]
    [InlineData(typeof(Cyclone), 6, 9)]
    [InlineData(typeof(SoulGlaive), 8, 11)]
    public void ThrowingWeapon_ThrowRange_IsCorrect(Type weaponType, int expectedMin, int expectedMax)
    {
        var weapon = (BaseThrown)Activator.CreateInstance(weaponType);
        try
        {
            Assert.Equal(expectedMin, weapon.MinThrowRange);
            Assert.Equal(expectedMax, weapon.MaxThrowRange);
        }
        finally
        {
            weapon.Delete();
        }
    }

    // Race restriction

    [Theory]
    [InlineData(typeof(Boomerang))]
    [InlineData(typeof(Cyclone))]
    [InlineData(typeof(SoulGlaive))]
    public void ThrowingWeapon_RequiredRaces_IsGargoylesOnly(Type weaponType)
    {
        var weapon = (BaseThrown)Activator.CreateInstance(weaponType);
        try
        {
            Assert.Equal(Race.AllowGargoylesOnly, weapon.RequiredRaces);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Theory]
    [InlineData(typeof(Boomerang))]
    [InlineData(typeof(Cyclone))]
    [InlineData(typeof(SoulGlaive))]
    public void ThrowingWeapon_CheckRace_AllowsGargoyle(Type weaponType)
    {
        var weapon = (BaseThrown)Activator.CreateInstance(weaponType);
        var mobile = CreateMobile(Map.Felucca, new Point3D(5700, 500, 0));
        try
        {
            mobile.Race = Race.Gargoyle;
            Assert.True(weapon.CheckRace(mobile, message: false));
        }
        finally
        {
            weapon.Delete();
            mobile.Delete();
        }
    }

    [Theory]
    [InlineData(typeof(Boomerang))]
    [InlineData(typeof(Cyclone))]
    [InlineData(typeof(SoulGlaive))]
    public void ThrowingWeapon_CheckRace_BlocksHuman(Type weaponType)
    {
        var weapon = (BaseThrown)Activator.CreateInstance(weaponType);
        var mobile = CreateMobile(Map.Felucca, new Point3D(5720, 500, 0));
        try
        {
            mobile.Race = Race.Human;
            Assert.False(weapon.CheckRace(mobile, message: false));
        }
        finally
        {
            weapon.Delete();
            mobile.Delete();
        }
    }

    [Theory]
    [InlineData(typeof(Boomerang))]
    [InlineData(typeof(Cyclone))]
    [InlineData(typeof(SoulGlaive))]
    public void ThrowingWeapon_CheckRace_BlocksElf(Type weaponType)
    {
        var weapon = (BaseThrown)Activator.CreateInstance(weaponType);
        var mobile = CreateMobile(Map.Felucca, new Point3D(5740, 500, 0));
        try
        {
            mobile.Race = Race.Elf;
            Assert.False(weapon.CheckRace(mobile, message: false));
        }
        finally
        {
            weapon.Delete();
            mobile.Delete();
        }
    }

    // AbyssReaver

    [Fact]
    public void AbyssReaver_IsInstanceOfCyclone()
    {
        var weapon = new AbyssReaver();
        try
        {
            Assert.IsType<Cyclone>(weapon, exactMatch: false);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void AbyssReaver_LabelNumber_IsAbyssReaver()
    {
        var weapon = new AbyssReaver();
        try
        {
            Assert.Equal(1112694, weapon.LabelNumber);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void AbyssReaver_Slayer_IsExorcism()
    {
        var weapon = new AbyssReaver();
        try
        {
            Assert.Equal(SlayerName.Exorcism, weapon.Slayer);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void AbyssReaver_WeaponDamage_IsInRange()
    {
        var weapon = new AbyssReaver();
        try
        {
            Assert.InRange(weapon.Attributes.WeaponDamage, 25, 35);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void AbyssReaver_ThrowingSkillBonus_IsInRange()
    {
        var weapon = new AbyssReaver();
        try
        {
            weapon.SkillBonuses.GetValues(0, out var skill, out var bonus);
            Assert.Equal(SkillName.Throwing, skill);
            Assert.InRange(bonus, 5.0, 10.0);
        }
        finally
        {
            weapon.Delete();
        }
    }

    // Hit chance modifiers

    /// <summary>At optimal range with no shield the chance should not change.</summary>
    [Fact]
    public void ModifyHitChance_OptimalRange_NoShield_NoChange()
    {
        var map = Map.Felucca;
        var attacker = CreateMobile(map, new Point3D(5000, 500, 0));
        var defender = CreateMobile(map, new Point3D(5005, 500, 0)); // distance 5, within range 4..7
        var weapon = new TestThrown();

        try
        {
            attacker.Skills.Throwing.BaseFixedPoint = 0;
            attacker.RawDex = 10;

            var result = weapon.TestModifyHitChance(attacker, defender, 0.8);

            Assert.Equal(0.8, result, 10);
        }
        finally
        {
            attacker.Delete();
            defender.Delete();
            weapon.Delete();
        }
    }

    /// <summary>
    /// At distance 1 with no throwing skill and minimum dex, close to the full -12% applies.
    /// Elf race avoids the Human Jack-of-All-Trades 20.0 skill floor.
    /// RawDex clamps to 1 minimum, so mitigation = (0+1)/20 = 0.05 -> penalty = 0.1195.
    /// </summary>
    [Fact]
    public void ModifyHitChance_CloseQuarters_MinimumSkill_AppliesNearFullPenalty()
    {
        var map = Map.Felucca;
        var attacker = CreateMobile(map, new Point3D(5100, 500, 0));
        var defender = CreateMobile(map, new Point3D(5101, 500, 0)); // distance 1
        var weapon = new TestThrown();

        try
        {
            attacker.Race = Race.Elf; // humans have Jack-of-All-Trades (+20.0 floor on all skills)
            attacker.Skills.Throwing.BaseFixedPoint = 0;
            attacker.RawDex = 1; // minimum

            var result = weapon.TestModifyHitChance(attacker, defender, 0.8);

            Assert.Equal(0.6805, result, 10);
        }
        finally
        {
            attacker.Delete();
            defender.Delete();
            weapon.Delete();
        }
    }

    /// <summary>At distance 0 with 120 skill and 120 dex, (120+120)/20 = 12 caps mitigation, no penalty.</summary>
    [Fact]
    public void ModifyHitChance_CloseQuarters_MaxSkillAndDex_NoChange()
    {
        var map = Map.Felucca;
        var attacker = CreateMobile(map, new Point3D(5200, 500, 0));
        var defender = CreateMobile(map, new Point3D(5200, 500, 0)); // same tile, distance 0
        var weapon = new TestThrown();

        try
        {
            attacker.Skills.Throwing.BaseFixedPoint = 1200; // 120.0
            attacker.RawDex = 120;

            var result = weapon.TestModifyHitChance(attacker, defender, 0.8);

            Assert.Equal(0.8, result, 10);
        }
        finally
        {
            attacker.Delete();
            defender.Delete();
            weapon.Delete();
        }
    }

    /// <summary>Distance 2 is above melee range but below MinThrowRange of 4, so the flat -12% applies.</summary>
    [Fact]
    public void ModifyHitChance_BelowMinRange_AppliesFlatPenalty()
    {
        var map = Map.Felucca;
        var attacker = CreateMobile(map, new Point3D(5300, 500, 0));
        var defender = CreateMobile(map, new Point3D(5302, 500, 0)); // distance 2
        var weapon = new TestThrown();

        try
        {
            var result = weapon.TestModifyHitChance(attacker, defender, 0.8);

            Assert.Equal(0.68, result, 10);
        }
        finally
        {
            attacker.Delete();
            defender.Delete();
            weapon.Delete();
        }
    }

    /// <summary>
    /// Shield at 100 Parry: 1200/100 = 12% penalty, chance * 0.88.
    /// Layer must be set explicitly since tile data is not loaded in tests.
    /// </summary>
    [Fact]
    public void ModifyHitChance_Shield_100Parry_AppliesCorrectPenalty()
    {
        var map = Map.Felucca;
        var attacker = CreateMobile(map, new Point3D(5400, 500, 0));
        var defender = CreateMobile(map, new Point3D(5405, 500, 0)); // distance 5, optimal
        var weapon = new TestThrown();
        var shield = new Buckler { Layer = Layer.TwoHanded }; // tile data not loaded in tests

        try
        {
            attacker.Skills.Parry.BaseFixedPoint = 1000; // 100.0
            attacker.AddItem(shield);

            var result = weapon.TestModifyHitChance(attacker, defender, 0.8);

            Assert.Equal(0.704, result, 10);
        }
        finally
        {
            attacker.Delete();
            defender.Delete();
            weapon.Delete();
        }
    }

    /// <summary>Shield at 120 Parry: 1200/120 = 10% penalty, chance * 0.90.</summary>
    [Fact]
    public void ModifyHitChance_Shield_120Parry_AppliesCorrectPenalty()
    {
        var map = Map.Felucca;
        var attacker = CreateMobile(map, new Point3D(5500, 500, 0));
        var defender = CreateMobile(map, new Point3D(5505, 500, 0)); // distance 5, optimal
        var weapon = new TestThrown();
        var shield = new Buckler { Layer = Layer.TwoHanded };

        try
        {
            attacker.Skills.Parry.BaseFixedPoint = 1200; // 120.0
            attacker.AddItem(shield);

            var result = weapon.TestModifyHitChance(attacker, defender, 0.8);

            Assert.Equal(0.72, result, 10);
        }
        finally
        {
            attacker.Delete();
            defender.Delete();
            weapon.Delete();
        }
    }

    /// <summary>
    /// Shield at 0 Parry hits the 90% cap, chance * 0.10.
    /// Elf race prevents JOAT from raising Parry to 20.0.
    /// </summary>
    [Fact]
    public void ModifyHitChance_Shield_ZeroParry_CapsAt90Percent()
    {
        var map = Map.Felucca;
        var attacker = CreateMobile(map, new Point3D(5600, 500, 0));
        var defender = CreateMobile(map, new Point3D(5605, 500, 0)); // distance 5, optimal
        var weapon = new TestThrown();
        var shield = new Buckler { Layer = Layer.TwoHanded };

        try
        {
            attacker.Race = Race.Elf; // humans have Jack-of-All-Trades (+20.0 floor on all skills)
            attacker.Skills.Parry.BaseFixedPoint = 0;
            attacker.AddItem(shield);

            var result = weapon.TestModifyHitChance(attacker, defender, 0.8);

            Assert.Equal(0.08, result, 10);
        }
        finally
        {
            attacker.Delete();
            defender.Delete();
            weapon.Delete();
        }
    }

    // Throwing artifacts

    [Fact]
    public void ValkyriesGlaive_IsInstanceOfSoulGlaive()
    {
        var weapon = new ValkyriesGlaive();
        try
        {
            Assert.IsType<SoulGlaive>(weapon, exactMatch: false);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void ValkyriesGlaive_LabelNumber_IsCorrect()
    {
        var weapon = new ValkyriesGlaive();
        try
        {
            Assert.Equal(1113531, weapon.LabelNumber);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void ValkyriesGlaive_Slayer_IsSilver()
    {
        var weapon = new ValkyriesGlaive();
        try
        {
            Assert.Equal(SlayerName.Silver, weapon.Slayer);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void ValkyriesGlaive_InitHits_Is255()
    {
        var weapon = new ValkyriesGlaive();
        try
        {
            Assert.Equal(255, weapon.InitMinHits);
            Assert.Equal(255, weapon.InitMaxHits);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void BansheesCall_IsInstanceOfCyclone()
    {
        var weapon = new BansheesCall();
        try
        {
            Assert.IsType<Cyclone>(weapon, exactMatch: false);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void BansheesCall_LabelNumber_IsCorrect()
    {
        var weapon = new BansheesCall();
        try
        {
            Assert.Equal(1113529, weapon.LabelNumber);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void BansheesCall_ElementDamage_IsColdOnly()
    {
        var weapon = new BansheesCall();
        try
        {
            Assert.Equal(100, weapon.AosElementDamages.Cold);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void StormCaller_IsInstanceOfBoomerang()
    {
        var weapon = new StormCaller();
        try
        {
            Assert.IsType<Boomerang>(weapon, exactMatch: false);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void StormCaller_LabelNumber_IsCorrect()
    {
        var weapon = new StormCaller();
        try
        {
            Assert.Equal(1113530, weapon.LabelNumber);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void StormCaller_ElementDamages_AreEqualFiveSplit()
    {
        var weapon = new StormCaller();
        try
        {
            Assert.Equal(20, weapon.AosElementDamages.Physical);
            Assert.Equal(20, weapon.AosElementDamages.Fire);
            Assert.Equal(20, weapon.AosElementDamages.Cold);
            Assert.Equal(20, weapon.AosElementDamages.Poison);
            Assert.Equal(20, weapon.AosElementDamages.Energy);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void RaptorClaw_IsInstanceOfBoomerang()
    {
        var weapon = new RaptorClaw();
        try
        {
            Assert.IsType<Boomerang>(weapon, exactMatch: false);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void RaptorClaw_LabelNumber_IsCorrect()
    {
        var weapon = new RaptorClaw();
        try
        {
            Assert.Equal(1112394, weapon.LabelNumber);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void RaptorClaw_Slayer_IsSilver()
    {
        var weapon = new RaptorClaw();
        try
        {
            Assert.Equal(SlayerName.Silver, weapon.Slayer);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void StoneSlithClaw_IsInstanceOfCyclone()
    {
        var weapon = new StoneSlithClaw();
        try
        {
            Assert.IsType<Cyclone>(weapon, exactMatch: false);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void StoneSlithClaw_LabelNumber_IsCorrect()
    {
        var weapon = new StoneSlithClaw();
        try
        {
            Assert.Equal(1112393, weapon.LabelNumber);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void StoneSlithClaw_Slayer_IsDaemonDismissal()
    {
        var weapon = new StoneSlithClaw();
        try
        {
            Assert.Equal(SlayerName.DaemonDismissal, weapon.Slayer);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void WindOfCorruption_IsInstanceOfCyclone()
    {
        var weapon = new WindOfCorruption();
        try
        {
            Assert.IsType<Cyclone>(weapon, exactMatch: false);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void WindOfCorruption_LabelNumber_IsCorrect()
    {
        var weapon = new WindOfCorruption();
        try
        {
            Assert.Equal(1150358, weapon.LabelNumber);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void WindOfCorruption_Slayer_IsFey()
    {
        var weapon = new WindOfCorruption();
        try
        {
            Assert.Equal(SlayerName.Fey, weapon.Slayer);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void WindOfCorruption_ElementDamage_IsChaosOnly()
    {
        var weapon = new WindOfCorruption();
        try
        {
            Assert.Equal(100, weapon.AosElementDamages.Chaos);
        }
        finally
        {
            weapon.Delete();
        }
    }

    // MovingShot

    /// <summary>BaseMana was raised from 15 to 20 to match the OSI reference.</summary>
    [Fact]
    public void MovingShot_BaseMana_Is20()
    {
        Assert.Equal(20, WeaponAbility.MovingShot.BaseMana);
    }

    /// <summary>Pre-TOL accuracy penalty is -25; -35 applies only on TOL+ servers.</summary>
    [Fact]
    public void MovingShot_AccuracyBonus_IsNegative25_PreTOL()
    {
        if (!Core.TOL)
        {
            Assert.Equal(-25, WeaponAbility.MovingShot.AccuracyBonus);
        }
    }

    // Loot tables

    [Fact]
    public void SAWeaponTypes_ContainsAllExpectedTypes()
    {
        Assert.Contains(typeof(DiscMace), Loot.SAWeaponTypes);
        Assert.Contains(typeof(GargishTalwar), Loot.SAWeaponTypes);
        Assert.Contains(typeof(DualPointedSpear), Loot.SAWeaponTypes);
        Assert.Contains(typeof(GlassStaff), Loot.SAWeaponTypes);
        Assert.Contains(typeof(DualShortAxes), Loot.SAWeaponTypes);
        Assert.Contains(typeof(GlassSword), Loot.SAWeaponTypes);
    }

    [Fact]
    public void SARangedWeaponTypes_ContainsAllThrowingWeapons()
    {
        Assert.Contains(typeof(Boomerang), Loot.SARangedWeaponTypes);
        Assert.Contains(typeof(Cyclone), Loot.SARangedWeaponTypes);
        Assert.Contains(typeof(SoulGlaive), Loot.SARangedWeaponTypes);
    }

    [Fact]
    public void SARangedWeaponTypes_ContainsExactlyThreeEntries()
    {
        Assert.Equal(3, Loot.SARangedWeaponTypes.Length);
    }

    private static PlayerMobile CreateMobile(Map map, Point3D location)
    {
        var mobile = new PlayerMobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.MoveToWorld(location, map);
        return mobile;
    }
}
