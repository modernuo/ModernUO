using System;
using System.Collections.Generic;
using System.Globalization;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HitManaDrainPropertyTests
{
    private const int HitManaDrainCliloc = 1113699;
    private static readonly Point3D TestLocation = new(6200, 520, 0);

    [Fact]
    public void ExtendedWeaponAttributes_StoresAndDupesHitManaDrain()
    {
        var weapon = new TestKatana();
        var dupe = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.HitManaDrain = 70;

            weapon.Dupe(dupe);

            Assert.Equal(70, weapon.ExtendedWeaponAttributes.HitManaDrain);
            Assert.Equal(70, dupe.ExtendedWeaponAttributes.HitManaDrain);
        }
        finally
        {
            weapon.Delete();
            dupe.Delete();
        }
    }

    [Fact]
    public void ExtendedWeaponAttributes_GetProperties_GatesHitManaDrainToStygianAbyss()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.HitManaDrain = 70;

            Core.Expansion = Expansion.ML;
            var mondainsLegacy = new RecordingPropertyList();
            weapon.ExtendedWeaponAttributes.GetProperties(mondainsLegacy);
            Assert.DoesNotContain(mondainsLegacy.Entries, entry => entry.Number == HitManaDrainCliloc);

            Core.Expansion = Expansion.SA;
            var stygianAbyss = new RecordingPropertyList();
            weapon.ExtendedWeaponAttributes.GetProperties(stygianAbyss);
            Assert.Contains(stygianAbyss.Entries, entry =>
                entry.Number == HitManaDrainCliloc && entry.Argument == "70"
            );
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OnHit_AtMaximumIntensityDrainsPlayerAndCreatureMana(bool playerDefender)
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana { TestDamage = 10 };
        var attacker = CreateMobile(mana: 50);
        var defender = CreateMobile(player: playerDefender, mana: 50, location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.SA;
            weapon.ExtendedWeaponAttributes.HitManaDrain = 70;

            using var random = new PredictableRandom(0);
            weapon.OnHit(attacker, defender);

            Assert.Equal(50, attacker.Mana);
            Assert.Equal(48, defender.Mana);
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
    public void OnHit_UsesHitManaDrainIntensityAsProcChance()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana { TestDamage = 10 };
        var attacker = CreateMobile();
        var defender = CreateMobile(mana: 50, location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.SA;
            weapon.ExtendedWeaponAttributes.HitManaDrain = 70;

            using (var proc = new PredictableRandom(69))
            {
                weapon.OnHit(attacker, defender);
            }

            Assert.Equal(48, defender.Mana);
            defender.Mana = 50;

            using (var noProc = new PredictableRandom(70))
            {
                weapon.OnHit(attacker, defender);
            }

            Assert.Equal(50, defender.Mana);
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
    public void OnHit_UsesAppliedDamageWithFloorAndDoesNotDrainBelowZero()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile();
        var defender = CreateMobile(mana: 100, location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.SA;
            weapon.ExtendedWeaponAttributes.HitManaDrain = 70;

            using (var random = new PredictableRandom(0))
            {
                weapon.TestDamage = 4;
                weapon.OnHit(attacker, defender);
            }

            Assert.Equal(100, defender.Mana);

            using (var random = new PredictableRandom(0))
            {
                weapon.TestDamage = 5;
                weapon.OnHit(attacker, defender);
            }

            Assert.Equal(99, defender.Mana);
            defender.Mana = 3;

            using (var random = new PredictableRandom(0))
            {
                weapon.TestDamage = 50;
                weapon.OnHit(attacker, defender);
            }

            Assert.Equal(0, defender.Mana);
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
    public void OnHit_PreStygianAbyssDoesNotDrainMana()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana { TestDamage = 10 };
        var attacker = CreateMobile();
        var defender = CreateMobile(mana: 50, location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.ML;
            weapon.ExtendedWeaponAttributes.HitManaDrain = 70;

            using var random = new PredictableRandom(0);
            weapon.OnHit(attacker, defender);

            Assert.Equal(50, defender.Mana);
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
    public void RunicAttributeGeneration_DoesNotRollHitManaDrain()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            Core.Expansion = Expansion.SA;

            BaseRunicTool.ApplyAttributesTo(weapon, false, 0, 25, 100, 100);

            Assert.Equal(0, weapon.ExtendedWeaponAttributes.HitManaDrain);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    private static Mobile CreateMobile(bool player = false, int mana = 0, Point3D location = default)
    {
        var mobile = new Mobile(World.NewMobile) { Player = player };
        mobile.DefaultMobileInit();
        mobile.RawStr = 300;
        mobile.RawDex = 100;
        mobile.RawInt = 100;
        mobile.Hits = mobile.HitsMax;
        mobile.Stam = mobile.StamMax;
        mobile.Mana = mana;
        mobile.MoveToWorld(location == default ? TestLocation : location, Map.Felucca);
        return mobile;
    }

    private sealed class TestKatana : Katana
    {
        public int TestDamage { get; set; } = 1;

        public override bool CheckHit(Mobile attacker, Mobile defender) => true;
        public override int ComputeDamage(Mobile attacker, Mobile defender) => TestDamage;
        public override int AbsorbDamage(Mobile attacker, Mobile defender, int damage) => damage;
        public override void AddBlood(Mobile attacker, Mobile defender, int damage)
        {
        }
    }

    private sealed record PropertyEntry(int Number, string Argument);

    private sealed class RecordingPropertyList : IPropertyList
    {
        private string _interpolated = string.Empty;

        public List<PropertyEntry> Entries { get; } = [];

        public void Reset()
        {
        }

        public void Terminate()
        {
        }

        public void Add(int number) => Entries.Add(new PropertyEntry(number, string.Empty));
        public void Add(int number, string argument) => Entries.Add(new PropertyEntry(number, argument));
        public void Add(ReadOnlySpan<char> argument) => Entries.Add(new PropertyEntry(0, argument.ToString()));
        public void Add(int number, ReadOnlySpan<char> argument) => Entries.Add(new PropertyEntry(number, argument.ToString()));
        public void AddChunked(ReadOnlySpan<char> text) => Entries.Add(new PropertyEntry(0, text.ToString()));
        public OplTextBlock TextBlock() => new(this);
        public void Add(int number, int value) => Entries.Add(new PropertyEntry(number, value.ToString(CultureInfo.InvariantCulture)));
        public void AddLocalized(int value) => Entries.Add(new PropertyEntry(0, value.ToString(CultureInfo.InvariantCulture)));
        public void AddLocalized(int number, int value) => Entries.Add(new PropertyEntry(number, value.ToString(CultureInfo.InvariantCulture)));
        public void Add(ref IPropertyList.InterpolatedStringHandler handler) => Entries.Add(new PropertyEntry(0, _interpolated));
        public void Add(int number, ref IPropertyList.InterpolatedStringHandler handler) =>
            Entries.Add(new PropertyEntry(number, _interpolated));
        public void InitializeInterpolation(int literalLength, int formattedCount) => _interpolated = string.Empty;
        public void AppendLiteral(string value) => _interpolated += value;
        public void AppendFormatted<T>(T value) => _interpolated += value;
        public void AppendFormatted<T>(T value, string format) =>
            _interpolated += value is IFormattable formattable ? formattable.ToString(format, null) : value;
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
