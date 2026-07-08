using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BanePropertyTests
{
    private const int BaneCliloc = 1154671;

    [Fact]
    public void WeaponAttributes_StoresAndDupesBane()
    {
        var weapon = new TestKatana();
        var dupe = new TestKatana();

        try
        {
            weapon.WeaponAttributes.Bane = 1;

            weapon.Dupe(dupe);

            Assert.Equal(1, weapon.WeaponAttributes.Bane);
            Assert.Equal(1, dupe.WeaponAttributes.Bane);
        }
        finally
        {
            weapon.Delete();
            dupe.Delete();
        }
    }

    [Fact]
    public void WeaponAttributes_GetProperties_GatesBaneTooltipToHighSeas()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            weapon.WeaponAttributes.Bane = 1;

            Core.Expansion = Expansion.ML;
            var preHighSeas = new RecordingPropertyList();
            weapon.WeaponAttributes.GetProperties(preHighSeas);
            Assert.DoesNotContain(preHighSeas.Numbers, number => number == BaneCliloc);

            Core.Expansion = Expansion.HS;
            var highSeas = new RecordingPropertyList();
            weapon.WeaponAttributes.GetProperties(highSeas);
            Assert.Contains(highSeas.Numbers, number => number == BaneCliloc);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    [Theory]
    [InlineData(12000, 6001, 0)]
    [InlineData(12000, 6000, 0)]
    [InlineData(12000, 5999, 175)]
    [InlineData(150, 74, 22)]
    [InlineData(150, 1, 44)]
    public void GetBaneDamage_UsesBelowHalfHealthThresholdCapAndFlooring(int hitsMax, int hits, int expected)
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var defender = CreateMobile(hitsMax, hits);

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.WeaponAttributes.Bane = 1;

            Assert.Equal(expected, weapon.GetBaneDamage(defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_PreHighSeas_DoesNotApplyBane()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile(150, 150);
        var defender = CreateMobile(12000, 5999);

        try
        {
            Core.Expansion = Expansion.ML;
            weapon.WeaponAttributes.Bane = 1;
            AddResistance(defender, ResistanceType.Physical, 70);
            var before = defender.Hits;

            weapon.OnHit(attacker, defender);

            Assert.Equal(1, before - defender.Hits); // only the guaranteed 1 minimum weapon damage
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_BelowHalfHealth_AppliesPhysicalBaneDamageAfterResistance()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile(150, 150);
        var defender = CreateMobile(12000, 5999);

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.WeaponAttributes.Bane = 1;
            AddResistance(defender, ResistanceType.Physical, 70);
            var before = defender.Hits;

            weapon.OnHit(attacker, defender);

            Assert.Equal(53, before - defender.Hits); // 1 normal minimum + floor(175 raw Bane * 30%)
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_PlayerTarget_UsesSameBaneFormulaAndMitigation()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile(150, 150);
        var defender = CreatePlayerMobile(150, 74);

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.WeaponAttributes.Bane = 1;
            AddResistance(defender, ResistanceType.Physical, 70);
            var before = defender.Hits;

            weapon.OnHit(attacker, defender);

            Assert.Equal(7, before - defender.Hits); // 1 normal minimum + floor(22 raw Bane * 30%)
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_ElementalWeapon_BaneStillUsesPhysicalResistance()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile(150, 150);
        var defender = CreateMobile(12000, 5999);

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.WeaponAttributes.Bane = 1;
            weapon.AosElementDamages.Fire = 100;
            AddResistance(defender, ResistanceType.Physical, 70);
            AddResistance(defender, ResistanceType.Fire, 0);
            var before = defender.Hits;

            weapon.OnHit(attacker, defender);

            Assert.Equal(53, before - defender.Hits); // Bane remains physical instead of following 100% fire split
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_RangedWeapon_AppliesBane()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestBow();
        var attacker = CreateMobile(150, 150);
        var defender = CreateMobile(12000, 5999);

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.WeaponAttributes.Bane = 1;
            AddResistance(defender, ResistanceType.Physical, 70);
            var before = defender.Hits;

            weapon.OnHit(attacker, defender);

            Assert.Equal(53, before - defender.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_CurrentWeaponAbility_DoesNotSuppressBaneAndDoesNotBypassPhysicalResistance()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile(150, 150);
        var defender = CreateMobile(12000, 5999);

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.WeaponAttributes.Bane = 1;
            AddResistance(defender, ResistanceType.Physical, 70);
            WeaponAbility.SetCurrentAbility(attacker, WeaponAbility.ArmorIgnore);
            var before = defender.Hits;

            weapon.OnHit(attacker, defender);

            Assert.Equal(53, before - defender.Hits); // Armor Ignore affects the weapon hit, not Bane's physical mitigation
        }
        finally
        {
            WeaponAbility.ClearCurrentAbility(attacker);
            Core.Expansion = previousExpansion;
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_ZeroDamageAfterParryStyleAbsorb_DoesNotApplyBane()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana { AbsorbAllDamage = true };
        var attacker = CreateMobile(150, 150);
        var defender = CreateMobile(12000, 5999);

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.WeaponAttributes.Bane = 1;
            AddResistance(defender, ResistanceType.Physical, 70);
            var before = defender.Hits;

            weapon.OnHit(attacker, defender);

            Assert.Equal(before, defender.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnSwing_MissDoesNotApplyBane()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana { Hit = false };
        var attacker = CreateMobile(150, 150, Map.Felucca, new Point3D(6200, 500, 0));
        var defender = CreateMobile(12000, 5999, Map.Felucca, new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.WeaponAttributes.Bane = 1;
            var before = defender.Hits;

            weapon.OnSwing(attacker, defender);

            Assert.Equal(before, defender.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_AppliesBanePerSuccessfulWeaponHit()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile(150, 150);
        var defender = CreateMobile(12000, 5999);

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.WeaponAttributes.Bane = 1;
            AddResistance(defender, ResistanceType.Physical, 70);
            var before = defender.Hits;

            weapon.OnHit(attacker, defender);
            weapon.OnHit(attacker, defender);

            Assert.Equal(106, before - defender.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void RunicAttributeGeneration_DoesNotRollBane()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            Core.Expansion = Expansion.HS;

            BaseRunicTool.ApplyAttributesTo(weapon, false, 0, 25, 100, 100);

            Assert.Equal(0, weapon.WeaponAttributes.Bane);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    private static Mobile CreateMobile(int hitsMax, int hits, Map map = null, Point3D location = default)
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        InitializeHits(mobile, hitsMax, hits);

        if (map != null)
        {
            mobile.MoveToWorld(location, map);
        }

        return mobile;
    }

    private static Mobile CreatePlayerMobile(int hitsMax, int hits)
    {
        var mobile = new Mobile(World.NewMobile)
        {
            Player = true
        };

        mobile.DefaultMobileInit();
        InitializeHits(mobile, hitsMax, hits);
        return mobile;
    }

    private static void InitializeHits(Mobile mobile, int hitsMax, int hits)
    {
        mobile.RawStr = Math.Max(1, (hitsMax - 50) * 2);
        Assert.Equal(hitsMax, mobile.HitsMax);
        mobile.Hits = hits;
    }

    private static void AddResistance(Mobile mobile, ResistanceType type, int offset) =>
        mobile.AddResistanceMod(new ResistanceMod(type, $"BaneTest{type}", offset, mobile));

    private class TestKatana : Katana
    {
        public bool AbsorbAllDamage { get; init; }
        public bool Hit { get; init; } = true;

        public override bool CheckHit(Mobile attacker, Mobile defender) => Hit;
        public override int ComputeDamage(Mobile attacker, Mobile defender) => 1;
        public override int AbsorbDamage(Mobile attacker, Mobile defender, int damage) => AbsorbAllDamage ? 0 : damage;
        public override void AddBlood(Mobile attacker, Mobile defender, int damage)
        {
        }
    }

    private class TestBow : Bow
    {
        public override int ComputeDamage(Mobile attacker, Mobile defender) => 1;
        public override int AbsorbDamage(Mobile attacker, Mobile defender, int damage) => damage;
        public override void AddBlood(Mobile attacker, Mobile defender, int damage)
        {
        }
    }

    private sealed class RecordingPropertyList : IPropertyList
    {
        private string _interpolated = string.Empty;

        public List<int> Numbers { get; } = [];

        public void Reset()
        {
        }

        public void Terminate()
        {
        }

        public void Add(int number) => Numbers.Add(number);
        public void Add(int number, string argument) => Numbers.Add(number);
        public void Add(ReadOnlySpan<char> argument) => Numbers.Add(0);
        public void Add(int number, ReadOnlySpan<char> argument) => Numbers.Add(number);
        public void AddChunked(ReadOnlySpan<char> text) => Numbers.Add(0);
        public OplTextBlock TextBlock() => new(this);
        public void Add(int number, int value) => Numbers.Add(number);
        public void AddLocalized(int value) => Numbers.Add(0);
        public void AddLocalized(int number, int value) => Numbers.Add(number);
        public void Add(ref IPropertyList.InterpolatedStringHandler handler) => Numbers.Add(0);
        public void Add(int number, ref IPropertyList.InterpolatedStringHandler handler) => Numbers.Add(number);
        public void InitializeInterpolation(int literalLength, int formattedCount) => _interpolated = string.Empty;
        public void AppendLiteral(string value) => _interpolated += value;
        public void AppendFormatted<T>(T value) => _interpolated += value;
        public void AppendFormatted<T>(T value, string format) => _interpolated += value is IFormattable formattable ? formattable.ToString(format, null) : value;
        public void AppendFormatted<T>(T value, int alignment) => _interpolated += value;
        public void AppendFormatted<T>(T value, int alignment, string format) =>
            _interpolated += value is IFormattable formattable ? formattable.ToString(format, null) : value;
        public void AppendFormatted(ReadOnlySpan<char> value) => _interpolated += value.ToString();
        public void AppendFormatted(ReadOnlySpan<char> value, int alignment, string format = null) =>
            _interpolated += value.ToString();
        public void AppendFormatted(object value, int alignment = 0, string format = null) => _interpolated += value;
        public void AppendFormatted(string value) => _interpolated += value;
        public void AppendFormatted(string value, int alignment, string format = null) => _interpolated += value;
    }
}
