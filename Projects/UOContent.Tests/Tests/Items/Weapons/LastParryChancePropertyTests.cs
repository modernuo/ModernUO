using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Misc;
using Server.Tests;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class LastParryChancePropertyTests
{
    private const int LastParryChanceCliloc = 1158861;

    [Fact]
    public void ShieldSuccessfulParry_SetsRuntimeTooltipValueAndClearsOnRemoval()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var defender = CreateDefender(parry: 120.0, bushido: 0.0, dex: 100);
        var shield = new Buckler();

        try
        {
            EnsureSkillChecksConfigured();
            Core.Expansion = Expansion.EJ;

            Assert.True(defender.EquipItem(shield));
            Assert.True(BaseWeapon.CheckParry(defender));
            Assert.Equal(35, shield.LastParryChance);
            AssertShowsLastParryChance(shield, 35);

            shield.Internalize();

            Assert.Equal(0, shield.LastParryChance);
            AssertDoesNotShowLastParryChance(shield);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            shield.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void WeaponSuccessfulParry_SetsOneHandedAndTwoHandedRuntimeTooltipValues()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var oneHandedDefender = CreateDefender(parry: 120.0, bushido: 120.0, dex: 100);
        var twoHandedDefender = CreateDefender(parry: 120.0, bushido: 120.0, dex: 100);
        var katana = new Katana();
        var halberd = new Halberd();

        try
        {
            EnsureSkillChecksConfigured();
            Core.Expansion = Expansion.EJ;

            Assert.True(oneHandedDefender.EquipItem(katana));
            Assert.True(BaseWeapon.CheckParry(oneHandedDefender));
            Assert.Equal(35, katana.LastParryChance);
            AssertShowsLastParryChance(katana, 35);

            Assert.True(twoHandedDefender.EquipItem(halberd));
            Assert.True(BaseWeapon.CheckParry(twoHandedDefender));
            Assert.Equal(40, halberd.LastParryChance);
            AssertShowsLastParryChance(halberd, 40);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            katana.Delete();
            halberd.Delete();
            oneHandedDefender.Delete();
            twoHandedDefender.Delete();
        }
    }

    [Fact]
    public void WeaponFallbackParry_DisplaysActualSuccessfulAosChanceBranch()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var defender = CreateDefender(parry: 100.0, bushido: 0.0, dex: 100);
        var katana = new Katana();

        try
        {
            EnsureSkillChecksConfigured();
            Core.Expansion = Expansion.EJ;

            Assert.True(defender.EquipItem(katana));
            Assert.True(BaseWeapon.CheckParry(defender));

            Assert.Equal(17, katana.LastParryChance);
            AssertShowsLastParryChance(katana, 17);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            katana.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void TooltipDisplay_IsGatedToEndlessJourney()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new Katana { LastParryChance = 35 };
        var shield = new Buckler { LastParryChance = 35 };

        try
        {
            Core.Expansion = Expansion.TOL;
            AssertDoesNotShowLastParryChance(weapon);
            AssertDoesNotShowLastParryChance(shield);

            Core.Expansion = Expansion.EJ;
            AssertShowsLastParryChance(weapon, 35);
            AssertShowsLastParryChance(shield, 35);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            shield.Delete();
        }
    }

    [Fact]
    public void FistsAndRangedWeapons_DoNotReceiveOrDisplayLastParryChance()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var defender = CreateDefender(parry: 120.0, bushido: 120.0, dex: 100);
        var bow = new Bow();
        var fists = new Fists { LastParryChance = 35 };

        try
        {
            EnsureSkillChecksConfigured();
            Core.Expansion = Expansion.EJ;

            Assert.True(defender.EquipItem(bow));
            Assert.False(BaseWeapon.CheckParry(defender));
            Assert.Equal(0, bow.LastParryChance);
            AssertDoesNotShowLastParryChance(bow);
            AssertDoesNotShowLastParryChance(fists);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            bow.Delete();
            fists.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void RuntimeState_DoesNotUseRollablePropertyContainers()
    {
        var weapon = new Katana { LastParryChance = 35 };
        var shield = new Buckler { LastParryChance = 35 };

        try
        {
            Assert.True(weapon.Attributes.IsEmpty);
            Assert.True(weapon.WeaponAttributes.IsEmpty);
            Assert.True(weapon.ExtendedWeaponAttributes.IsEmpty);
            Assert.True(weapon.NegativeAttributes.IsEmpty);
            Assert.True(weapon.AosElementDamages.IsEmpty);

            Assert.True(shield.Attributes.IsEmpty);
            Assert.True(shield.ArmorAttributes.IsEmpty);
            Assert.True(shield.NegativeAttributes.IsEmpty);
            Assert.True(shield.AbsorptionAttributes.IsEmpty);
        }
        finally
        {
            weapon.Delete();
            shield.Delete();
        }
    }

    private static Mobile CreateDefender(double parry, double bushido, int dex)
    {
        var mobile = new Mobile { Player = true };
        mobile.DefaultMobileInit();
        mobile.InitStats(100, dex, 100);
        mobile.SkillsCap = 7200;
        mobile.Skills[SkillName.Parry].Cap = 120.0;
        mobile.Skills[SkillName.Bushido].Cap = 120.0;
        mobile.Skills[SkillName.Parry].Base = parry;
        mobile.Skills[SkillName.Bushido].Base = bushido;
        return mobile;
    }

    private static void EnsureSkillChecksConfigured()
    {
        if (AntiMacroSystem.Settings == null)
        {
            AntiMacroSystem.Configure();
        }

        SkillCheck.Configure();
        SkillCheck.Initialize();
    }

    private static void AssertShowsLastParryChance(Item item, int expectedValue)
    {
        var properties = new RecordingPropertyList();
        item.GetProperties(properties);

        Assert.Contains(
            properties.Entries,
            entry => entry.Number == LastParryChanceCliloc && entry.Argument == expectedValue.ToString()
        );
    }

    private static void AssertDoesNotShowLastParryChance(Item item)
    {
        var properties = new RecordingPropertyList();
        item.GetProperties(properties);

        Assert.DoesNotContain(properties.Entries, entry => entry.Number == LastParryChanceCliloc);
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
        public void Add(int number, int value) => Entries.Add(new PropertyEntry(number, value.ToString()));
        public void AddLocalized(int value) => Entries.Add(new PropertyEntry(0, value.ToString()));
        public void AddLocalized(int number, int value) => Entries.Add(new PropertyEntry(number, value.ToString()));
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
