using System;
using System.Collections.Generic;
using Server;
using Server.Collections;
using Server.ContextMenus;
using Server.Items;
using Server.Spells;
using Server.Tests;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class SpellFocusingSashTests
{
    private const int SpellFocusingCliloc = 1150058;
    private const int BrittleCliloc = 1116209;
    private const int ManaIncreaseCliloc = 1060439;
    private const int DefendChanceCliloc = 1060408;
    private const int StrengthRequirementCliloc = 1061170;
    private const int DurabilityCliloc = 1060639;

    [Fact]
    public void SpellFocusingSash_HasArtifactStatsAndPropertyOrder()
    {
        var previousExpansion = Core.Expansion;
        var sash = new SpellFocusingSash();

        try
        {
            Core.Expansion = Expansion.SA;
            var list = new RecordingPropertyList();
            sash.GetProperties(list);

            Assert.Equal(1.0, sash.DefaultWeight);
            Assert.Equal(1, sash.Attributes.BonusMana);
            Assert.Equal(5, sash.Attributes.DefendChance);
            Assert.Equal(10, sash.StrRequirement);
            Assert.Equal(255, sash.HitPoints);
            Assert.Equal(255, sash.MaxHitPoints);

            var spellFocusingIndex = list.Entries.FindIndex(entry => entry.Number == SpellFocusingCliloc);
            Assert.True(spellFocusingIndex > 0);
            Assert.Equal(1072788, list.Entries[spellFocusingIndex - 1].Number);
            Assert.True(spellFocusingIndex < list.Entries.FindIndex(entry => entry.Number == BrittleCliloc));
            Assert.Contains(list.Entries, entry => entry.Number == StrengthRequirementCliloc && entry.Argument == "10");
            Assert.Contains(list.Entries, entry => entry.Number == DurabilityCliloc && entry.Argument == "255\t255");
            Assert.True(
                list.Entries.FindIndex(entry => entry.Number == DefendChanceCliloc) <
                list.Entries.FindIndex(entry => entry.Number == ManaIncreaseCliloc)
            );
        }
        finally
        {
            Core.Expansion = previousExpansion;
            sash.Delete();
        }
    }

    [Fact]
    public void SpellFocusingSash_EnabledStateSerializesAndContextMenuTogglesIt()
    {
        var previousExpansion = Core.Expansion;
        var caster = CreateMobile(player: true);
        var sash = new SpellFocusingSash();
        var deserialized = new SpellFocusingSash();

        try
        {
            Core.Expansion = Expansion.SA;
            caster.AddItem(sash);

            var menu = ContextMenuSystem.CreateContextMenu(caster, sash);
            var entry = Assert.Single(menu.Entries, e => e.Number == 3006151);
            entry.OnClick(caster, sash);
            Assert.False(sash.Enabled);

            sash.Enabled = true;
            var writer = new BufferWriter(true);
            sash.Enabled = false;
            sash.Serialize(writer);
            var buffer = new byte[writer.Position];
            writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);
            deserialized.Deserialize(new BufferReader(buffer));
            Assert.False(deserialized.Enabled);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            sash.Delete();
            deserialized.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void SpellFocusingSash_AppliesPvMSequenceAndResetsAfterPeak()
    {
        var previousExpansion = Core.Expansion;
        var caster = CreateMobile(player: true);
        var target = CreateMobile(player: false);
        var sash = new SpellFocusingSash();
        var spell = new TestSpell(caster);

        try
        {
            Core.Expansion = Expansion.SA;
            caster.AddItem(sash);
            var expected = new[] { -30, -24, -18, -12, -6, 0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30 };

            foreach (var expectedOffset in expected)
            {
                Assert.True(SpellFocusingSash.TryGetDamageOffset(spell, caster, target, out var actual));
                Assert.Equal(expectedOffset, actual);
            }

            Assert.True(SpellFocusingSash.TryGetDamageOffset(spell, caster, target, out var resetOffset));
            Assert.Equal(-30, resetOffset);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            sash.Delete();
            target.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void SpellFocusingSash_HoldsPvPCapForFiveSpellsAndResetsOnTargetChange()
    {
        var previousExpansion = Core.Expansion;
        var caster = CreateMobile(player: true);
        var target = CreateMobile(player: true);
        var secondTarget = CreateMobile(player: true);
        var sash = new SpellFocusingSash();
        var spell = new TestSpell(caster);

        try
        {
            Core.Expansion = Expansion.SA;
            caster.AddItem(sash);

            for (var i = 0; i < 15; i++)
            {
                Assert.True(SpellFocusingSash.TryGetDamageOffset(spell, caster, target, out _));
            }

            for (var i = 0; i < 6; i++)
            {
                Assert.True(SpellFocusingSash.TryGetDamageOffset(spell, caster, target, out var offset));
                Assert.Equal(20, offset);
            }

            Assert.True(SpellFocusingSash.TryGetDamageOffset(spell, caster, target, out var resetOffset));
            Assert.Equal(-30, resetOffset);

            Assert.True(SpellFocusingSash.TryGetDamageOffset(spell, caster, secondTarget, out var targetResetOffset));
            Assert.Equal(-30, targetResetOffset);

            sash.Enabled = false;
            Assert.False(SpellFocusingSash.TryGetDamageOffset(spell, caster, target, out _));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            sash.Delete();
            secondTarget.Delete();
            target.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void SpellFocusingSash_ExcludesNonQualifyingSpells()
    {
        var previousExpansion = Core.Expansion;
        var caster = CreateMobile(player: true);
        var target = CreateMobile(player: false);
        var sash = new SpellFocusingSash();

        try
        {
            Core.Expansion = Expansion.SA;
            caster.AddItem(sash);
            Assert.False(SpellFocusingSash.TryGetDamageOffset(new NonQualifyingSpell(caster), caster, target, out _));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            sash.Delete();
            target.Delete();
            caster.Delete();
        }
    }

    private static Mobile CreateMobile(bool player)
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.Player = player;
        mobile.RawStr = 100;
        mobile.Hits = mobile.HitsMax;
        return mobile;
    }

    private sealed class TestSpell : Spell
    {
        private static readonly SpellInfo TestInfo = new("Test Spell", "test");

        public TestSpell(Mobile caster) : base(caster, null, TestInfo)
        {
        }

        public override bool SpellFocusingEligible => true;
        public override TimeSpan CastDelayBase => TimeSpan.Zero;
        public override void OnCast()
        {
        }

        public override int GetMana() => 0;
    }

    private sealed class NonQualifyingSpell : Spell
    {
        private static readonly SpellInfo TestInfo = new("Non-Qualifying Spell", "no");

        public NonQualifyingSpell(Mobile caster) : base(caster, null, TestInfo)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.Zero;
        public override void OnCast()
        {
        }

        public override int GetMana() => 0;
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
        public void Add(int number, ref IPropertyList.InterpolatedStringHandler handler) => Entries.Add(new PropertyEntry(number, _interpolated));
        public void InitializeInterpolation(int literalLength, int formattedCount) => _interpolated = string.Empty;
        public void AppendLiteral(string value) => _interpolated += value;
        public void AppendFormatted<T>(T value) => _interpolated += value;
        public void AppendFormatted<T>(T value, string format) => _interpolated += value is IFormattable formattable ? formattable.ToString(format, null) : value;
        public void AppendFormatted<T>(T value, int alignment) => _interpolated += value;
        public void AppendFormatted<T>(T value, int alignment, string format) => _interpolated += value is IFormattable formattable ? formattable.ToString(format, null) : value;
        public void AppendFormatted(ReadOnlySpan<char> value) => _interpolated += value.ToString();
        public void AppendFormatted(ReadOnlySpan<char> value, int alignment, string format = null) => _interpolated += value.ToString();
        public void AppendFormatted(object value, int alignment = 0, string format = null) => _interpolated += value;
        public void AppendFormatted(string value) => _interpolated += value;
        public void AppendFormatted(string value, int alignment, string format = null) => _interpolated += value;
    }
}
