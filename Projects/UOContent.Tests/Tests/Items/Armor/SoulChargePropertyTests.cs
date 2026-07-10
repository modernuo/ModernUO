using System;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Items;
using Server.Tests;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class SoulChargePropertyTests
{
    private const int SoulChargeCliloc = 1113630;
    private static readonly Point3D TestLocation = new(6200, 500, 0);

    [Fact]
    public void AosArmorAttributes_StoresDupesAndSerializesSoulChargeOnShield()
    {
        var shield = new BronzeShield();
        var dupe = new BronzeShield();
        var deserialized = new BronzeShield();

        try
        {
            shield.ArmorAttributes.SoulCharge = 30;

            shield.Dupe(dupe);

            var writer = new BufferWriter(true);
            shield.Serialize(writer);
            var buffer = new byte[writer.Position];
            writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

            var reader = new BufferReader(buffer);
            deserialized.Deserialize(reader);

            Assert.Equal(30, shield.ArmorAttributes.SoulCharge);
            Assert.Equal(30, dupe.ArmorAttributes.SoulCharge);
            Assert.Equal(buffer.Length, reader.Position);
            Assert.Equal(30, deserialized.ArmorAttributes.SoulCharge);
        }
        finally
        {
            shield.Delete();
            dupe.Delete();
            deserialized.Delete();
        }
    }

    [Fact]
    public void AosArmorAttributes_SoulChargeIsStaffEditable()
    {
        var property = typeof(AosArmorAttributes).GetProperty(nameof(AosArmorAttributes.SoulCharge));

        Assert.NotNull(property);
        var attribute = property.GetCustomAttribute<CommandPropertyAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(AccessLevel.GameMaster, attribute.ReadLevel);
        Assert.Equal(AccessLevel.GameMaster, attribute.WriteLevel);
    }

    [Fact]
    public void BaseShield_GetProperties_GatesSoulChargeTooltipToStygianAbyss()
    {
        var previousExpansion = Core.Expansion;
        var shield = new BronzeShield();
        var armor = new LeatherChest();

        try
        {
            shield.ArmorAttributes.SoulCharge = 30;
            armor.ArmorAttributes[AosArmorAttribute.SoulCharge] = 30;

            Core.Expansion = Expansion.ML;
            var preStygianAbyss = new RecordingPropertyList();
            shield.GetProperties(preStygianAbyss);
            Assert.DoesNotContain(preStygianAbyss.Entries, entry => entry.Number == SoulChargeCliloc);

            Core.Expansion = Expansion.SA;
            var stygianAbyss = new RecordingPropertyList();
            shield.GetProperties(stygianAbyss);
            Assert.Contains(
                stygianAbyss.Entries,
                entry => entry.Number == SoulChargeCliloc && entry.Argument == "30"
            );

            var nonShield = new RecordingPropertyList();
            armor.GetProperties(nonShield);
            Assert.DoesNotContain(nonShield.Entries, entry => entry.Number == SoulChargeCliloc);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            shield.Delete();
            armor.Delete();
        }
    }

    [Fact]
    public void DamageTaken_SoulChargeConvertsActualPostResistDamageToMana()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var defender = CreateMobile(mana: 0);
        var source = CreateMobile(location: new Point3D(6201, 500, 0));
        var shield = EquipSoulChargeShield(defender, 50);

        try
        {
            Core.Expansion = Expansion.SA;
            AddResistance(defender, ResistanceType.Physical, 50);

            AOS.Damage(defender, source, 100, false, 100, 0, 0, 0, 0);

            Assert.Equal(450, defender.Hits);
            Assert.Equal(15, defender.Mana);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            shield.Delete();
            defender.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void DamageTaken_SoulChargeManaGainIsCappedAtMissingMana()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var defender = CreateMobile(mana: 90);
        var source = CreateMobile(location: new Point3D(6201, 500, 0));
        var shield = EquipSoulChargeShield(defender, 50);

        try
        {
            Core.Expansion = Expansion.SA;

            AOS.Damage(defender, source, 100, false, 100, 0, 0, 0, 0);

            Assert.Equal(100, defender.Mana);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            shield.Delete();
            defender.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void DamageTaken_PreStygianAbyssSoulChargeIsInert()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var defender = CreateMobile();
        var source = CreateMobile(location: new Point3D(6201, 500, 0));
        var shield = EquipSoulChargeShield(defender, 50);

        try
        {
            Core.Expansion = Expansion.ML;

            AOS.Damage(defender, source, 100, false, 100, 0, 0, 0, 0);

            Assert.Equal(0, defender.Mana);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            shield.Delete();
            defender.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void DamageTaken_SoulChargeRequiresAnEquippedShieldAndLivingDefender()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var nonShieldDefender = CreateMobile();
        var nonShield = new LeatherChest();
        var unequippedDefender = CreateMobile();
        var unequippedShield = EquipSoulChargeShield(unequippedDefender, 50);
        var deadDefender = CreateMobile();
        var deadShield = EquipSoulChargeShield(deadDefender, 50);
        var deletedDefender = CreateMobile();
        var deletedShield = EquipSoulChargeShield(deletedDefender, 50);
        var source = CreateMobile(location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.SA;
            nonShield.ArmorAttributes[AosArmorAttribute.SoulCharge] = 50;
            nonShieldDefender.AddItem(nonShield);

            AOS.Damage(nonShieldDefender, source, 100, false, 100, 0, 0, 0, 0);
            Assert.Equal(0, nonShieldDefender.Mana);

            unequippedDefender.RemoveItem(unequippedShield);
            AOS.Damage(unequippedDefender, source, 100, false, 100, 0, 0, 0, 0);
            Assert.Equal(0, unequippedDefender.Mana);

            deadDefender.Kill();
            AOS.Damage(deadDefender, source, 100, false, 100, 0, 0, 0, 0);
            Assert.Equal(0, deadDefender.Mana);

            deletedDefender.Delete();
            AOS.Damage(deletedDefender, source, 100, false, 100, 0, 0, 0, 0);
            Assert.Equal(0, deletedDefender.Mana);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            nonShield.Delete();
            unequippedShield.Delete();
            deadShield.Delete();
            deletedShield.Delete();
            nonShieldDefender.Delete();
            unequippedDefender.Delete();
            deadDefender.Delete();
            deletedDefender.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void DamageTaken_SoulChargeDoesNotRestoreForZeroDamageOrFailedRoll()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(99);
        var defender = CreateMobile();
        var source = CreateMobile(location: new Point3D(6201, 500, 0));
        var shield = EquipSoulChargeShield(defender, 50);

        try
        {
            Core.Expansion = Expansion.SA;

            AOS.Damage(defender, source, 0, false, 100, 0, 0, 0, 0);
            AOS.Damage(defender, source, 100, false, 100, 0, 0, 0, 0);

            Assert.Equal(0, defender.Mana);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            shield.Delete();
            defender.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void DamageTaken_SoulChargeUsesPublishedChanceCap()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(50);
        var defender = CreateMobile();
        var source = CreateMobile(location: new Point3D(6201, 500, 0));
        var shield = EquipSoulChargeShield(defender, 100);

        try
        {
            Core.Expansion = Expansion.SA;

            AOS.Damage(defender, source, 100, false, 100, 0, 0, 0, 0);

            Assert.Equal(0, defender.Mana);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            shield.Delete();
            defender.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void RunicAttributeGeneration_DoesNotRollSoulCharge()
    {
        var previousExpansion = Core.Expansion;
        var armor = new LeatherChest();
        var shield = new BronzeShield();

        try
        {
            Core.Expansion = Expansion.SA;

            BaseRunicTool.ApplyAttributesTo(armor, false, 0, 25, 100, 100);
            BaseRunicTool.ApplyAttributesTo(shield, false, 0, 25, 100, 100);

            Assert.Equal(0, armor.ArmorAttributes.SoulCharge);
            Assert.Equal(0, shield.ArmorAttributes.SoulCharge);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            armor.Delete();
            shield.Delete();
        }
    }

    private static BronzeShield EquipSoulChargeShield(Mobile mobile, int value)
    {
        var shield = new BronzeShield();
        shield.ArmorAttributes.SoulCharge = value;
        mobile.AddItem(shield);
        Assert.Same(shield, mobile.FindItemOnLayer<BaseShield>(Layer.TwoHanded));
        return shield;
    }

    private static Mobile CreateMobile(
        int hitsMax = 500,
        int hits = 500,
        int manaMax = 100,
        int mana = 0,
        Point3D location = default
    )
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.RawStr = Math.Max(1, (hitsMax - 50) * 2);
        mobile.RawInt = manaMax;
        Assert.Equal(hitsMax, mobile.HitsMax);
        Assert.Equal(manaMax, mobile.ManaMax);
        mobile.Hits = hits;
        mobile.Mana = mana;
        mobile.MoveToWorld(location == default ? TestLocation : location, Map.Felucca);
        return mobile;
    }

    private static void AddResistance(Mobile mobile, ResistanceType type, int offset) =>
        mobile.AddResistanceMod(new ResistanceMod(type, $"SoulChargeTest{type}{mobile.Serial}", offset, mobile));

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
        public void Add(ref IPropertyList.InterpolatedStringHandler handler) =>
            Entries.Add(new PropertyEntry(0, _interpolated));
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
