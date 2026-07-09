using System;
using System.Collections.Generic;
using Server;
using Server.Engines.Craft;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BrittlePropertyTests
{
    private const int BrittleCliloc = 1116209;

    [Fact]
    public void NegativeAttributes_StoresDupesAndSerializesBrittleOnSupportedFamilies()
    {
        AssertStoresAndDupesBrittle(new TestKatana(), new TestKatana());
        AssertStoresAndDupesBrittle(new LeatherChest(), new LeatherChest());
        AssertStoresAndDupesBrittle(new Buckler(), new Buckler());

        AssertSerializesBrittle(new TestKatana(), new TestKatana());
        AssertSerializesBrittle(new LeatherChest(), new LeatherChest());
        AssertSerializesBrittle(new Buckler(), new Buckler());
    }

    [Fact]
    public void NegativeAttributes_DoesNotStoreBrittleOnUnsupportedJewelry()
    {
        var ring = new GoldRing();

        try
        {
            ring.NegativeAttributes.Brittle = 1;

            Assert.Equal(0, ring.NegativeAttributes.Brittle);
        }
        finally
        {
            ring.Delete();
        }
    }

    [Fact]
    public void GetProperties_GatesBrittleTooltipToHighSeasOnSupportedFamilies()
    {
        var previousExpansion = Core.Expansion;
        var items = CreateSupportedItems();

        try
        {
            foreach (var item in items)
            {
                GetNegativeAttributes(item).Brittle = 1;

                Core.Expansion = Expansion.SA;
                var preHighSeas = new RecordingPropertyList();
                item.GetProperties(preHighSeas);
                Assert.DoesNotContain(preHighSeas.Entries, entry => entry.Number == BrittleCliloc);

                Core.Expansion = Expansion.HS;
                var highSeas = new RecordingPropertyList();
                item.GetProperties(highSeas);
                Assert.Contains(highSeas.Entries, entry => entry.Number == BrittleCliloc);
            }
        }
        finally
        {
            Core.Expansion = previousExpansion;
            DeleteItems(items);
        }
    }

    [Fact]
    public void PowderOfTemperament_FortifiesNonBrittleRepairableItem()
    {
        var previousExpansion = Core.Expansion;
        var player = CreatePlayerMobile(new Point3D(6400, 500, 0));
        var weapon = CreateDamagedWeapon();
        var powder = new PowderOfTemperament();

        try
        {
            Core.Expansion = Expansion.HS;
            player.Backpack.AddItem(weapon);
            player.Backpack.AddItem(powder);

            UsePowder(player, powder, weapon);

            Assert.Equal(9, powder.UsesRemaining);
            Assert.True(weapon.MaxHitPoints > 100);
            Assert.True(weapon.HitPoints > 50);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            powder.Delete();
            player.Delete();
        }
    }

    [Fact]
    public void PowderOfTemperament_RejectsBrittleWeaponAndArmorWithoutMutatingDurabilityOrCharges()
    {
        var previousExpansion = Core.Expansion;
        var player = CreatePlayerMobile(new Point3D(6420, 500, 0));
        var weapon = CreateDamagedWeapon();
        var armor = CreateDamagedArmor();
        var shield = CreateDamagedShield();
        var powder = new PowderOfTemperament();

        try
        {
            Core.Expansion = Expansion.HS;
            player.Backpack.AddItem(weapon);
            player.Backpack.AddItem(armor);
            player.Backpack.AddItem(shield);
            player.Backpack.AddItem(powder);

            weapon.NegativeAttributes.Brittle = 1;
            armor.NegativeAttributes.Brittle = 1;
            shield.NegativeAttributes.Brittle = 1;

            AssertPowderRejectsBrittle(player, powder, weapon);
            AssertPowderRejectsBrittle(player, powder, armor);
            AssertPowderRejectsBrittle(player, powder, shield);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            armor.Delete();
            shield.Delete();
            powder.Delete();
            player.Delete();
        }
    }

    [Fact]
    public void Brittle_IsNoOpBeforeHighSeasForPowderUse()
    {
        var previousExpansion = Core.Expansion;
        var player = CreatePlayerMobile(new Point3D(6440, 500, 0));
        var weapon = CreateDamagedWeapon();
        var powder = new PowderOfTemperament();

        try
        {
            Core.Expansion = Expansion.SA;
            player.Backpack.AddItem(weapon);
            player.Backpack.AddItem(powder);
            weapon.NegativeAttributes.Brittle = 1;

            UsePowder(player, powder, weapon);

            Assert.False(NegativeAttributes.IsBrittle(weapon));
            Assert.Equal(9, powder.UsesRemaining);
            Assert.True(weapon.MaxHitPoints > 100);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            powder.Delete();
            player.Delete();
        }
    }

    [Fact]
    public void RepairFlow_StillRepairsBrittleWeaponsArmorAndShields()
    {
        var previousExpansion = Core.Expansion;
        using var random = new Server.Tests.PredictableRandom(0);
        var player = CreatePlayerMobile(new Point3D(6460, 500, 0));
        var weapon = CreateDamagedWeapon();
        var armor = CreateDamagedArmor();
        var shield = CreateDamagedShield();
        var smithHammer = new SmithHammer();
        var sewingKit = new SewingKit();
        var anvil = new Anvil();
        var forge = new Forge();

        try
        {
            Core.Expansion = Expansion.HS;
            SkillCheck.Configure();
            SkillCheck.Initialize();
            player.SkillsCap = 7200;
            player.Skills[SkillName.Blacksmith].Cap = 120.0;
            player.Skills[SkillName.Tailoring].Cap = 120.0;
            player.Skills[SkillName.Blacksmith].Base = 120.0;
            player.Skills[SkillName.Tailoring].Base = 120.0;
            player.Backpack.AddItem(weapon);
            player.Backpack.AddItem(armor);
            player.Backpack.AddItem(shield);
            player.Backpack.AddItem(smithHammer);
            player.Backpack.AddItem(sewingKit);
            anvil.MoveToWorld(player.Location, player.Map);
            forge.MoveToWorld(player.Location, player.Map);

            weapon.NegativeAttributes.Brittle = 1;
            armor.NegativeAttributes.Brittle = 1;
            shield.NegativeAttributes.Brittle = 1;

            RepairWithTool(player, GetOrInitBlacksmithySystem(), smithHammer, weapon);
            RepairWithTool(player, GetOrInitTailoringSystem(), sewingKit, armor);
            RepairWithTool(player, GetOrInitBlacksmithySystem(), smithHammer, shield);

            Assert.Equal(weapon.MaxHitPoints, weapon.HitPoints);
            Assert.Equal(armor.MaxHitPoints, armor.HitPoints);
            Assert.Equal(shield.MaxHitPoints, shield.HitPoints);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            armor.Delete();
            shield.Delete();
            smithHammer.Delete();
            sewingKit.Delete();
            anvil.Delete();
            forge.Delete();
            player.Delete();
        }
    }

    [Fact]
    public void RunicAttributeGeneration_DoesNotRollBrittle()
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

            Assert.Equal(0, weapon.NegativeAttributes.Brittle);
            Assert.Equal(0, armor.NegativeAttributes.Brittle);
            Assert.Equal(0, shield.NegativeAttributes.Brittle);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            armor.Delete();
            shield.Delete();
        }
    }

    private static void AssertStoresAndDupesBrittle<TItem>(TItem source, TItem dupe) where TItem : Item
    {
        try
        {
            GetNegativeAttributes(source).Brittle = 1;

            source.Dupe(dupe);

            Assert.Equal(1, GetNegativeAttributes(source).Brittle);
            Assert.Equal(1, GetNegativeAttributes(dupe).Brittle);
        }
        finally
        {
            source.Delete();
            dupe.Delete();
        }
    }

    private static void AssertSerializesBrittle<TItem>(TItem source, TItem deserialized) where TItem : Item
    {
        try
        {
            GetNegativeAttributes(source).Brittle = 1;

            var writer = new BufferWriter(true);
            source.Serialize(writer);
            var buffer = new byte[writer.Position];
            writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

            var reader = new BufferReader(buffer);
            deserialized.Deserialize(reader);

            Assert.Equal(buffer.Length, reader.Position);
            Assert.Equal(1, GetNegativeAttributes(deserialized).Brittle);
        }
        finally
        {
            source.Delete();
            deserialized.Delete();
        }
    }

    private static void AssertPowderRejectsBrittle<TItem>(Mobile player, PowderOfTemperament powder, TItem item)
        where TItem : Item, IDurability
    {
        var originalUses = powder.UsesRemaining;
        var originalMaxHitPoints = item.MaxHitPoints;
        var originalHitPoints = item.HitPoints;

        UsePowder(player, powder, item);

        Assert.Equal(originalUses, powder.UsesRemaining);
        Assert.Equal(originalMaxHitPoints, item.MaxHitPoints);
        Assert.Equal(originalHitPoints, item.HitPoints);
    }

    private static Katana CreateDamagedWeapon()
    {
        return new Katana
        {
            MaxHitPoints = 100,
            HitPoints = 50
        };
    }

    private static LeatherChest CreateDamagedArmor()
    {
        return new LeatherChest
        {
            MaxHitPoints = 100,
            HitPoints = 50
        };
    }

    private static Buckler CreateDamagedShield()
    {
        return new Buckler
        {
            MaxHitPoints = 100,
            HitPoints = 50
        };
    }

    private static void UsePowder(Mobile player, PowderOfTemperament powder, Item item)
    {
        powder.OnDoubleClick(player);
        Assert.NotNull(player.Target);
        player.Target.Invoke(player, item);
    }

    private static void RepairWithTool(Mobile player, CraftSystem craftSystem, BaseTool tool, Item item)
    {
        Repair.Do(player, craftSystem, tool);
        Assert.NotNull(player.Target);
        player.Target.Invoke(player, item);
    }

    private static CraftSystem GetOrInitBlacksmithySystem()
    {
        if (DefBlacksmithy.CraftSystem == null)
        {
            DefBlacksmithy.Initialize();
        }

        return DefBlacksmithy.CraftSystem;
    }

    private static CraftSystem GetOrInitTailoringSystem()
    {
        if (DefTailoring.CraftSystem == null)
        {
            DefTailoring.Initialize();
        }

        return DefTailoring.CraftSystem;
    }

    private static PlayerMobile CreatePlayerMobile(Point3D location)
    {
        var mobile = new PlayerMobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.MoveToWorld(location, Map.Felucca);
        mobile.AddItem(new Backpack());
        return mobile;
    }

    private static Item[] CreateSupportedItems() =>
    [
        new TestKatana(),
        new LeatherChest(),
        new Buckler()
    ];

    private static NegativeAttributes GetNegativeAttributes(Item item) => item switch
    {
        BaseWeapon weapon => weapon.NegativeAttributes,
        BaseArmor armor   => armor.NegativeAttributes,
        _                 => throw new ArgumentException($"Unsupported Brittle host: {item.GetType().FullName}", nameof(item))
    };

    private static void DeleteItems(IEnumerable<Item> items)
    {
        foreach (var item in items)
        {
            item.Delete();
        }
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
