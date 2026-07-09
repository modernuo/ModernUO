using System;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class MassivePropertyTests
{
    private const int StrengthRequirementCliloc = 1061170;
    private const int LowerRequirementsCliloc = 1060435;

    [Fact]
    public void NegativeAttributes_StoresDupesAndSerializesMassiveOnSupportedFamilies()
    {
        AssertStoresAndDupesMassive(new TestKatana(), new TestKatana());
        AssertStoresAndDupesMassive(new LeatherChest(), new LeatherChest());
        AssertStoresAndDupesMassive(new Buckler(), new Buckler());

        AssertSerializesMassive(new TestKatana(), new TestKatana());
        AssertSerializesMassive(new LeatherChest(), new LeatherChest());
        AssertSerializesMassive(new Buckler(), new Buckler());
    }

    [Fact]
    public void NegativeAttributes_DoesNotStoreMassiveOnUnsupportedJewelry()
    {
        var ring = new GoldRing();

        try
        {
            ring.NegativeAttributes.Massive = 1;

            Assert.Equal(0, ring.NegativeAttributes.Massive);
        }
        finally
        {
            ring.Delete();
        }
    }

    [Fact]
    public void Massive_RequiresAndDisplaysStrengthRequirement125InHighSeas()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana { StrRequirement = 10 };
        var armor = new LeatherChest { StrRequirement = 10 };
        var shield = new Buckler { StrRequirement = 10 };

        try
        {
            Core.Expansion = Expansion.HS;

            weapon.NegativeAttributes.Massive = 1;
            armor.NegativeAttributes.Massive = 1;
            shield.NegativeAttributes.Massive = 1;

            Assert.Equal(AOS.MassiveStrengthRequirement, weapon.ComputeStrengthRequirement());
            Assert.Equal(AOS.MassiveStrengthRequirement, armor.ComputeStatReq(StatType.Str));
            Assert.Equal(AOS.MassiveStrengthRequirement, shield.ComputeStatReq(StatType.Str));

            AssertShowsStrengthRequirement125WithoutMassiveRow(weapon);
            AssertShowsStrengthRequirement125WithoutMassiveRow(armor);
            AssertShowsStrengthRequirement125WithoutMassiveRow(shield);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            armor.Delete();
            shield.Delete();
        }
    }

    [Fact]
    public void Massive_BypassesLowerRequirementsForStrengthOnlyInHighSeas()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana { StrRequirement = 10 };
        var armor = new LeatherChest { StrRequirement = 10, DexRequirement = 10, IntRequirement = 10 };
        var shield = new Buckler { StrRequirement = 10, DexRequirement = 10, IntRequirement = 10 };

        try
        {
            Core.Expansion = Expansion.HS;

            weapon.NegativeAttributes.Massive = 1;
            weapon.WeaponAttributes.LowerStatReq = 100;

            armor.NegativeAttributes.Massive = 1;
            armor.ArmorAttributes.LowerStatReq = 100;

            shield.NegativeAttributes.Massive = 1;
            shield.ArmorAttributes.LowerStatReq = 100;

            Assert.Equal(AOS.MassiveStrengthRequirement, weapon.ComputeStrengthRequirement());
            Assert.Equal(AOS.MassiveStrengthRequirement, armor.ComputeStatReq(StatType.Str));
            Assert.Equal(AOS.MassiveStrengthRequirement, shield.ComputeStatReq(StatType.Str));
            Assert.Equal(0, armor.ComputeStatReq(StatType.Dex));
            Assert.Equal(0, armor.ComputeStatReq(StatType.Int));
            Assert.Equal(0, shield.ComputeStatReq(StatType.Dex));
            Assert.Equal(0, shield.ComputeStatReq(StatType.Int));

            AssertShowsStrengthAndLowerRequirements(weapon);
            AssertShowsStrengthAndLowerRequirements(armor);
            AssertShowsStrengthAndLowerRequirements(shield);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            armor.Delete();
            shield.Delete();
        }
    }

    [Fact]
    public void Massive_EquipChecksUseUnscaledStrengthRequirement125InHighSeas()
    {
        var previousExpansion = Core.Expansion;
        var weak = CreateMobile(str: 124);
        var strong = CreateMobile(str: 125);
        var weakWeapon = CreateMassiveWeapon();
        var strongWeapon = CreateMassiveWeapon();
        var weakArmor = CreateMassiveArmor();
        var strongArmor = CreateMassiveArmor();
        var weakShield = CreateMassiveShield();
        var strongShield = CreateMassiveShield();

        try
        {
            Core.Expansion = Expansion.HS;

            Assert.False(weakWeapon.CanEquip(weak));
            Assert.True(strongWeapon.CanEquip(strong));
            Assert.False(weakArmor.CanEquip(weak));
            Assert.True(strongArmor.CanEquip(strong));
            Assert.False(weakShield.CanEquip(weak));
            Assert.True(strongShield.CanEquip(strong));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weak.Delete();
            strong.Delete();
            weakWeapon.Delete();
            strongWeapon.Delete();
            weakArmor.Delete();
            strongArmor.Delete();
            weakShield.Delete();
            strongShield.Delete();
        }
    }

    [Fact]
    public void Massive_IsNoOpBeforeHighSeas()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana { StrRequirement = 10 };
        var armor = new LeatherChest { StrRequirement = 10 };
        var mobile = CreateMobile(str: 10);

        try
        {
            Core.Expansion = Expansion.SA;

            weapon.NegativeAttributes.Massive = 1;
            armor.NegativeAttributes.Massive = 1;

            Assert.False(NegativeAttributes.IsMassive(weapon));
            Assert.False(NegativeAttributes.IsMassive(armor));
            Assert.Equal(10, weapon.ComputeStrengthRequirement());
            Assert.Equal(10, armor.ComputeStatReq(StatType.Str));
            Assert.True(weapon.CanEquip(mobile));
            Assert.True(armor.CanEquip(mobile));

            var weaponProperties = new RecordingPropertyList();
            weapon.GetProperties(weaponProperties);
            Assert.Contains(weaponProperties.Entries, entry => entry.Number == StrengthRequirementCliloc && entry.Argument == "10");

            var armorProperties = new RecordingPropertyList();
            armor.GetProperties(armorProperties);
            Assert.Contains(armorProperties.Entries, entry => entry.Number == StrengthRequirementCliloc && entry.Argument == "10");
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            armor.Delete();
            mobile.Delete();
        }
    }

    [Fact]
    public void ValidateEquipment_DropsEquippedMassiveItemsWhenStrengthFallsBelow125()
    {
        var previousExpansion = Core.Expansion;
        var player = CreatePlayerMobile(str: 125, new Point3D(6300, 500, 0));
        var weapon = CreateMassiveWeapon();
        var armor = CreateMassiveArmor();
        var shield = CreateMassiveShield();

        try
        {
            Core.Expansion = Expansion.HS;

            Assert.True(player.EquipItem(weapon));
            Assert.True(player.EquipItem(armor));
            Assert.True(player.EquipItem(shield));

            player.RawStr = 124;
            InvokeValidateEquipmentSandbox(player);

            Assert.Same(player.Backpack, weapon.Parent);
            Assert.Same(player.Backpack, armor.Parent);
            Assert.Same(player.Backpack, shield.Parent);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            armor.Delete();
            shield.Delete();
            player.Delete();
        }
    }

    [Fact]
    public void RunicAttributeGeneration_DoesNotRollMassive()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var armor = new LeatherChest();
        var shield = new Buckler();

        try
        {
            Core.Expansion = Expansion.HS;

            BaseRunicTool.ApplyAttributesTo(weapon, false, 0, 25, 100, 100);
            BaseRunicTool.ApplyAttributesTo(armor, false, 0, 25, 100, 100);
            BaseRunicTool.ApplyAttributesTo(shield, false, 0, 25, 100, 100);

            Assert.Equal(0, weapon.NegativeAttributes.Massive);
            Assert.Equal(0, armor.NegativeAttributes.Massive);
            Assert.Equal(0, shield.NegativeAttributes.Massive);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            armor.Delete();
            shield.Delete();
        }
    }

    private static TestKatana CreateMassiveWeapon()
    {
        var weapon = new TestKatana { StrRequirement = 10 };
        weapon.NegativeAttributes.Massive = 1;
        weapon.WeaponAttributes.LowerStatReq = 100;
        return weapon;
    }

    private static LeatherChest CreateMassiveArmor()
    {
        var armor = new LeatherChest { StrRequirement = 10 };
        armor.NegativeAttributes.Massive = 1;
        armor.ArmorAttributes.LowerStatReq = 100;
        return armor;
    }

    private static Buckler CreateMassiveShield()
    {
        var shield = new Buckler { StrRequirement = 10 };
        shield.NegativeAttributes.Massive = 1;
        shield.ArmorAttributes.LowerStatReq = 100;
        return shield;
    }

    private static void AssertStoresAndDupesMassive<TItem>(TItem source, TItem dupe) where TItem : Item
    {
        try
        {
            GetNegativeAttributes(source).Massive = 1;

            source.Dupe(dupe);

            Assert.Equal(1, GetNegativeAttributes(source).Massive);
            Assert.Equal(1, GetNegativeAttributes(dupe).Massive);
        }
        finally
        {
            source.Delete();
            dupe.Delete();
        }
    }

    private static void AssertSerializesMassive<TItem>(TItem source, TItem deserialized) where TItem : Item
    {
        try
        {
            GetNegativeAttributes(source).Massive = 1;

            var writer = new BufferWriter(true);
            source.Serialize(writer);
            var buffer = new byte[writer.Position];
            writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

            var reader = new BufferReader(buffer);
            deserialized.Deserialize(reader);

            Assert.Equal(buffer.Length, reader.Position);
            Assert.Equal(1, GetNegativeAttributes(deserialized).Massive);
        }
        finally
        {
            source.Delete();
            deserialized.Delete();
        }
    }

    private static void AssertShowsStrengthRequirement125WithoutMassiveRow(Item item)
    {
        var properties = new RecordingPropertyList();
        item.GetProperties(properties);

        Assert.Contains(properties.Entries, entry =>
            entry.Number == StrengthRequirementCliloc && entry.Argument == AOS.MassiveStrengthRequirement.ToString());
        Assert.DoesNotContain(properties.Entries, entry => entry.Argument.Contains("Massive", StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertShowsStrengthAndLowerRequirements(Item item)
    {
        var properties = new RecordingPropertyList();
        item.GetProperties(properties);

        Assert.Contains(properties.Entries, entry =>
            entry.Number == StrengthRequirementCliloc && entry.Argument == AOS.MassiveStrengthRequirement.ToString());
        Assert.Contains(properties.Entries, entry => entry.Number == LowerRequirementsCliloc && entry.Argument == "100");
        Assert.DoesNotContain(properties.Entries, entry => entry.Argument.Contains("Massive", StringComparison.OrdinalIgnoreCase));
    }

    private static NegativeAttributes GetNegativeAttributes(Item item) => item switch
    {
        BaseWeapon weapon => weapon.NegativeAttributes,
        BaseArmor armor   => armor.NegativeAttributes,
        _                 => throw new ArgumentException($"Unsupported Massive host: {item.GetType().FullName}", nameof(item))
    };

    private static Mobile CreateMobile(int str)
    {
        var mobile = new Mobile(World.NewMobile)
        {
            Player = true
        };

        mobile.DefaultMobileInit();
        mobile.RawStr = str;
        mobile.RawDex = 125;
        mobile.RawInt = 125;
        return mobile;
    }

    private static PlayerMobile CreatePlayerMobile(int str, Point3D location)
    {
        var mobile = new PlayerMobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.RawStr = str;
        mobile.RawDex = 125;
        mobile.RawInt = 125;
        mobile.MoveToWorld(location, Map.Felucca);
        mobile.AddItem(new Backpack());
        return mobile;
    }

    private static void InvokeValidateEquipmentSandbox(PlayerMobile mobile)
    {
        var method = typeof(PlayerMobile).GetMethod(
            "ValidateEquipment_Sandbox",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        Assert.NotNull(method);
        method.Invoke(mobile, null);
    }

    private class TestKatana : Katana
    {
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
