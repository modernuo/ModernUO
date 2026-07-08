using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class PrizedPropertyTests
{
    private const int PrizedCliloc = 1154910;

    [Fact]
    public void NegativeAttributes_StoresAndDupesPrizedOnSupportedFamilies()
    {
        AssertStoresAndDupesPrized(new TestKatana(), new TestKatana());
        AssertStoresAndDupesPrized(new LeatherChest(), new LeatherChest());
        AssertStoresAndDupesPrized(new Buckler(), new Buckler());
        AssertStoresAndDupesPrized(new GoldRing(), new GoldRing());
    }

    [Fact]
    public void GetProperties_GatesPrizedTooltipToHighSeasOnSupportedFamilies()
    {
        var previousExpansion = Core.Expansion;
        var items = CreateSupportedItems();

        try
        {
            foreach (var item in items)
            {
                GetNegativeAttributes(item).Prized = 1;

                Core.Expansion = Expansion.ML;
                var preHighSeas = new RecordingPropertyList();
                item.GetProperties(preHighSeas);
                Assert.DoesNotContain(preHighSeas.Numbers, number => number == PrizedCliloc);

                Core.Expansion = Expansion.HS;
                var highSeas = new RecordingPropertyList();
                item.GetProperties(highSeas);
                Assert.Contains(highSeas.Numbers, number => number == PrizedCliloc);
            }
        }
        finally
        {
            Core.Expansion = previousExpansion;
            DeleteItems(items);
        }
    }

    [Fact]
    public void GetInsuranceCost_DoublesPrizedItemsOnlyInHighSeas()
    {
        var previousExpansion = Core.Expansion;
        var normal = new TestKatana();
        var prized = new TestKatana();

        try
        {
            prized.NegativeAttributes.Prized = 1;

            Core.Expansion = Expansion.ML;
            Assert.Equal(600, PlayerMobile.GetInsuranceCost(prized));
            Assert.False(NegativeAttributes.IsPrized(prized));

            Core.Expansion = Expansion.HS;
            Assert.Equal(600, PlayerMobile.GetInsuranceCost(normal));
            Assert.Equal(1200, PlayerMobile.GetInsuranceCost(prized));
            Assert.True(NegativeAttributes.IsPrized(prized));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            normal.Delete();
            prized.Delete();
        }
    }

    [Fact]
    public void Prized_DoesNotChangeBlessedCursedOrInsuranceState()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.NegativeAttributes.Prized = 1;

            Assert.Equal(LootType.Regular, weapon.LootType);
            Assert.False(weapon.Insured);
            Assert.False(weapon.PaidInsurance);
            Assert.Null(weapon.BlessedFor);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    [Fact]
    public void RunicAttributeGeneration_DoesNotRollPrized()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var armor = new LeatherChest();
        var jewel = new GoldRing();

        try
        {
            Core.Expansion = Expansion.HS;

            BaseRunicTool.ApplyAttributesTo(weapon, false, 0, 25, 100, 100);
            BaseRunicTool.ApplyAttributesTo(armor, false, 0, 25, 100, 100);
            BaseRunicTool.ApplyAttributesTo(jewel, false, 0, 25, 100, 100);

            Assert.Equal(0, weapon.NegativeAttributes.Prized);
            Assert.Equal(0, armor.NegativeAttributes.Prized);
            Assert.Equal(0, jewel.NegativeAttributes.Prized);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            armor.Delete();
            jewel.Delete();
        }
    }

    private static void AssertStoresAndDupesPrized<TItem>(TItem source, TItem dupe) where TItem : Item
    {
        try
        {
            GetNegativeAttributes(source).Prized = 1;

            source.Dupe(dupe);

            Assert.Equal(1, GetNegativeAttributes(source).Prized);
            Assert.Equal(1, GetNegativeAttributes(dupe).Prized);
        }
        finally
        {
            source.Delete();
            dupe.Delete();
        }
    }

    private static Item[] CreateSupportedItems() =>
    [
        new TestKatana(),
        new LeatherChest(),
        new Buckler(),
        new GoldRing()
    ];

    private static NegativeAttributes GetNegativeAttributes(Item item) => item switch
    {
        BaseWeapon weapon => weapon.NegativeAttributes,
        BaseArmor armor   => armor.NegativeAttributes,
        BaseJewel jewel   => jewel.NegativeAttributes,
        _                 => throw new ArgumentException($"Unsupported Prized host: {item.GetType().FullName}", nameof(item))
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
