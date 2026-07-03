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

        public int TestModifyDamage(Mobile attacker, Mobile defender, int damage) =>
            ModifyDamage(attacker, defender, damage);
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

    // DefMaxRange STR scaling (uses SoulGlaive: StrReq 60, Min 8, Max 11)

    [Fact]
    public void DefMaxRange_AtStrReq_EqualsMinThrowRange()
    {
        var weapon = new SoulGlaive();
        var attacker = CreateMobile(Map.Felucca, new Point3D(5800, 500, 0));
        try
        {
            attacker.RawStr = 60; // == AosStrengthReq
            attacker.AddItem(weapon);
            Assert.Equal(weapon.MinThrowRange, weapon.DefMaxRange); // 8
        }
        finally
        {
            weapon.Delete();
            attacker.Delete();
        }
    }

    [Fact]
    public void DefMaxRange_At140Str_EqualsMaxThrowRange()
    {
        var weapon = new SoulGlaive();
        var attacker = CreateMobile(Map.Felucca, new Point3D(5810, 500, 0));
        try
        {
            attacker.RawStr = 140;
            attacker.AddItem(weapon);
            Assert.Equal(weapon.MaxThrowRange, weapon.DefMaxRange); // 11
        }
        finally
        {
            weapon.Delete();
            attacker.Delete();
        }
    }

    [Fact]
    public void DefMaxRange_AboveMaxStr_IsCappedAtMaxThrowRange()
    {
        var weapon = new SoulGlaive();
        var attacker = CreateMobile(Map.Felucca, new Point3D(5820, 500, 0));
        try
        {
            attacker.RawStr = 200; // uncapped formula would give 13
            attacker.AddItem(weapon);
            Assert.Equal(weapon.MaxThrowRange, weapon.DefMaxRange); // 11, not 13
        }
        finally
        {
            weapon.Delete();
            attacker.Delete();
        }
    }

    [Fact]
    public void DefMaxRange_BelowStrReq_FlooredAtMinThrowRange()
    {
        var weapon = new SoulGlaive();
        var attacker = CreateMobile(Map.Felucca, new Point3D(5830, 500, 0));
        try
        {
            attacker.RawStr = 10; // below StrReq 60
            attacker.AddItem(weapon);
            Assert.Equal(weapon.MinThrowRange, weapon.DefMaxRange); // 8, not 6
        }
        finally
        {
            weapon.Delete();
            attacker.Delete();
        }
    }

    /// <summary>At the outermost admissible tile (dist == MaxRange) a throw loses 47% damage.</summary>
    [Fact]
    public void ModifyDamage_AtMaxRange_Reduces47Percent()
    {
        var map = Map.Felucca;
        var attacker = CreateMobile(map, new Point3D(5900, 500, 0));
        var weapon = new TestThrown(); // MinThrowRange 4 -> MaxThrowRange 7
        try
        {
            attacker.RawStr = 140;            // MaxRange == MaxThrowRange == 7
            attacker.AddItem(weapon);
            var defender = CreateMobile(map, new Point3D(5907, 500, 0)); // distance 7 == MaxRange
            try
            {
                Assert.Equal(53, weapon.TestModifyDamage(attacker, defender, 100));
            }
            finally
            {
                defender.Delete();
            }
        }
        finally
        {
            weapon.Delete();
            attacker.Delete();
        }
    }

    /// <summary>Inside max range, damage is unchanged.</summary>
    [Fact]
    public void ModifyDamage_WithinRange_NoChange()
    {
        var map = Map.Felucca;
        var attacker = CreateMobile(map, new Point3D(5920, 500, 0));
        var weapon = new TestThrown();
        try
        {
            attacker.RawStr = 140; // MaxRange 7
            attacker.AddItem(weapon);
            var defender = CreateMobile(map, new Point3D(5925, 500, 0)); // distance 5 < 7
            try
            {
                Assert.Equal(100, weapon.TestModifyDamage(attacker, defender, 100));
            }
            finally
            {
                defender.Delete();
            }
        }
        finally
        {
            weapon.Delete();
            attacker.Delete();
        }
    }

    private static PlayerMobile CreateMobile(Map map, Point3D location)
    {
        var mobile = new PlayerMobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.MoveToWorld(location, map);
        return mobile;
    }
}
