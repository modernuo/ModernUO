using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class UnwieldyPropertyTests
{
    private const int SingleWeightCliloc = 1072788;
    private const int MultipleWeightCliloc = 1072789;
    private const double OriginalWeight = 7.25;
    private const double UnwieldyWeight = 50;

    [Fact]
    public void NegativeAttribute_ReservesTheNextNonCollidingBitForUnwieldy()
    {
        Assert.True(Enum.TryParse<NegativeAttribute>("Unwieldy", out var unwieldy));
        Assert.Equal(0x00000010, (int)unwieldy);
    }

    [Fact]
    public void NegativeAttributes_StoresDupesAndSerializesUnwieldyOnSupportedFamilies()
    {
        var previousExpansion = Core.Expansion;

        try
        {
            Core.Expansion = Expansion.HS;

            AssertStoresDupesAndSerializesUnwieldy(new TestKatana(), new TestKatana(), new TestKatana());
            AssertStoresDupesAndSerializesUnwieldy(new LeatherChest(), new LeatherChest(), new LeatherChest());
            AssertStoresDupesAndSerializesUnwieldy(new Buckler(), new Buckler(), new Buckler());
        }
        finally
        {
            Core.Expansion = previousExpansion;
        }
    }

    [Fact]
    public void NegativeAttributes_DoesNotStoreUnwieldyOnUnsupportedJewelry()
    {
        var previousExpansion = Core.Expansion;
        var ring = new GoldRing { Weight = OriginalWeight };

        try
        {
            Core.Expansion = Expansion.HS;
            ring.NegativeAttributes.Unwieldy = 1;

            Assert.Equal(0, ring.NegativeAttributes.Unwieldy);
            Assert.Equal(OriginalWeight, ring.Weight);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            ring.Delete();
        }
    }

    [Fact]
    public void Unwieldy_UsesStandardWeightTotalsAndWeightPresentationInHighSeas()
    {
        var previousExpansion = Core.Expansion;

        try
        {
            Core.Expansion = Expansion.HS;

            AssertUsesStandardWeightTotalsAndPresentation(new TestKatana());
            AssertUsesStandardWeightTotalsAndPresentation(new LeatherChest());
            AssertUsesStandardWeightTotalsAndPresentation(new Buckler());
        }
        finally
        {
            Core.Expansion = previousExpansion;
        }
    }

    [Fact]
    public void Unwieldy_IsNoOpBeforeHighSeasIncludingAfterDeserialization()
    {
        var previousExpansion = Core.Expansion;
        var source = new TestKatana { Weight = OriginalWeight };
        var direct = new TestKatana { Weight = OriginalWeight };
        var deserialized = new TestKatana();

        try
        {
            Core.Expansion = Expansion.HS;
            source.NegativeAttributes.Unwieldy = 1;

            var writer = new BufferWriter(true);
            source.Serialize(writer);
            var buffer = new byte[writer.Position];
            writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

            Core.Expansion = Expansion.SA;
            direct.NegativeAttributes.Unwieldy = 1;

            Assert.Equal(1, direct.NegativeAttributes.Unwieldy);
            Assert.Equal(OriginalWeight, direct.Weight);
            AssertShowsWeightWithoutUnwieldyRow(direct, (int)Math.Ceiling(OriginalWeight));

            var reader = new BufferReader(buffer);
            deserialized.Deserialize(reader);

            Assert.Equal(buffer.Length, reader.Position);
            Assert.Equal(1, deserialized.NegativeAttributes.Unwieldy);
            Assert.Equal(OriginalWeight, deserialized.Weight);
            AssertShowsWeightWithoutUnwieldyRow(deserialized, (int)Math.Ceiling(OriginalWeight));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            source.Delete();
            direct.Delete();
            deserialized.Delete();
        }
    }

    [Fact]
    public void RunicAttributeGeneration_DoesNotRollUnwieldy()
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

            Assert.Equal(0, weapon.NegativeAttributes.Unwieldy);
            Assert.Equal(0, armor.NegativeAttributes.Unwieldy);
            Assert.Equal(0, shield.NegativeAttributes.Unwieldy);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            armor.Delete();
            shield.Delete();
        }
    }

    private static void AssertStoresDupesAndSerializesUnwieldy<TItem>(TItem source, TItem dupe, TItem deserialized)
        where TItem : Item
    {
        try
        {
            source.Weight = OriginalWeight;
            GetNegativeAttributes(source).Unwieldy = 1;

            source.Dupe(dupe);

            Assert.Equal(1, GetNegativeAttributes(dupe).Unwieldy);
            Assert.Equal(UnwieldyWeight, dupe.Weight);
            GetNegativeAttributes(dupe).Unwieldy = 0;
            Assert.Equal(OriginalWeight, dupe.Weight);

            var writer = new BufferWriter(true);
            source.Serialize(writer);
            var buffer = new byte[writer.Position];
            writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

            var reader = new BufferReader(buffer);
            deserialized.Deserialize(reader);

            Assert.Equal(buffer.Length, reader.Position);
            Assert.Equal(1, GetNegativeAttributes(deserialized).Unwieldy);
            Assert.Equal(UnwieldyWeight, deserialized.Weight);
            GetNegativeAttributes(deserialized).Unwieldy = 0;
            Assert.Equal(OriginalWeight, deserialized.Weight);
        }
        finally
        {
            source.Delete();
            dupe.Delete();
            deserialized.Delete();
        }
    }

    private static void AssertUsesStandardWeightTotalsAndPresentation(Item item)
    {
        var player = CreatePlayerMobile();

        try
        {
            item.Weight = OriginalWeight;
            player.Backpack.AddItem(item);

            var originalPileWeight = item.PileWeight;
            var originalContainerWeight = player.Backpack.TotalWeight;
            var originalMobileWeight = player.TotalWeight;

            GetNegativeAttributes(item).Unwieldy = 1;

            Assert.Equal(UnwieldyWeight, item.Weight);
            Assert.Equal((int)UnwieldyWeight, item.PileWeight);
            Assert.Equal(originalContainerWeight - originalPileWeight + (int)UnwieldyWeight, player.Backpack.TotalWeight);
            Assert.Equal(originalMobileWeight - originalPileWeight + (int)UnwieldyWeight, player.TotalWeight);
            AssertShowsWeightWithoutUnwieldyRow(item, (int)UnwieldyWeight);

            GetNegativeAttributes(item).Unwieldy = 2;
            Assert.Equal(1, GetNegativeAttributes(item).Unwieldy);
            Assert.Equal((int)UnwieldyWeight, item.PileWeight);
            Assert.Equal(originalContainerWeight - originalPileWeight + (int)UnwieldyWeight, player.Backpack.TotalWeight);
            Assert.Equal(originalMobileWeight - originalPileWeight + (int)UnwieldyWeight, player.TotalWeight);

            GetNegativeAttributes(item).Unwieldy = 0;

            Assert.Equal(OriginalWeight, item.Weight);
            Assert.Equal(originalPileWeight, item.PileWeight);
            Assert.Equal(originalContainerWeight, player.Backpack.TotalWeight);
            Assert.Equal(originalMobileWeight, player.TotalWeight);
            AssertShowsWeightWithoutUnwieldyRow(item, originalPileWeight);

            GetNegativeAttributes(item).Unwieldy = 0;
            Assert.Equal(0, GetNegativeAttributes(item).Unwieldy);
            Assert.Equal(OriginalWeight, item.Weight);
            Assert.Equal(originalContainerWeight, player.Backpack.TotalWeight);
            Assert.Equal(originalMobileWeight, player.TotalWeight);
        }
        finally
        {
            item.Delete();
            player.Delete();
        }
    }

    private static void AssertShowsWeightWithoutUnwieldyRow(Item item, int weight)
    {
        var properties = new RecordingPropertyList();
        item.GetProperties(properties);

        Assert.Contains(properties.Entries, entry =>
            entry.Number == (weight == 1 ? SingleWeightCliloc : MultipleWeightCliloc) && entry.Argument == weight.ToString());
        Assert.DoesNotContain(properties.Entries, entry =>
            entry.Argument.Contains("Unwieldy", StringComparison.OrdinalIgnoreCase));
    }

    private static NegativeAttributes GetNegativeAttributes(Item item) => item switch
    {
        BaseWeapon weapon => weapon.NegativeAttributes,
        BaseArmor armor   => armor.NegativeAttributes,
        _                 => throw new ArgumentException($"Unsupported Unwieldy host: {item.GetType().FullName}", nameof(item))
    };

    private static PlayerMobile CreatePlayerMobile()
    {
        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        player.AddItem(new Backpack());
        return player;
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
