using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Spells;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class SpellFocusingPropertyTests
{
    private const int SpellFocusingCliloc = 1150058;

    [Fact]
    public void AosAttributes_StoresAndDisplaysSpellFocusing()
    {
        var previousExpansion = Core.Expansion;
        var item = new Katana();

        try
        {
            Core.Expansion = Expansion.AOS;
            item.Attributes.SpellFocusing = 1;

            Assert.Equal(1, item.Attributes.SpellFocusing);

            var properties = new RecordingPropertyList();
            item.GetProperties(properties);

            Assert.Contains(properties.Numbers, number => number == SpellFocusingCliloc);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            item.Delete();
        }
    }

    [Fact]
    public void AosAttributes_DoesNotDisplaySpellFocusingBeforeAos()
    {
        var previousExpansion = Core.Expansion;
        var item = new Katana();

        try
        {
            item.Attributes.SpellFocusing = 1;
            Core.Expansion = Expansion.UOR;

            var properties = new RecordingPropertyList();
            item.GetProperties(properties);

            Assert.DoesNotContain(properties.Numbers, number => number == SpellFocusingCliloc);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            item.Delete();
        }
    }

    [Fact]
    public void SpellFocusing_UsesAnyEquippedItemWithTheProperty()
    {
        var previousExpansion = Core.Expansion;
        var caster = new Mobile(World.NewMobile);
        var target = new Mobile(World.NewMobile);
        var item = new Katana();
        var spell = new TestSpell(caster);

        try
        {
            Core.Expansion = Expansion.AOS;
            caster.DefaultMobileInit();
            caster.Player = true;
            target.DefaultMobileInit();
            target.Player = false;
            caster.AddItem(item);
            item.Attributes.SpellFocusing = 1;

            var expected = new[] { -30, -24, -18, -12, -6, 0, 2 };

            foreach (var expectedOffset in expected)
            {
                Assert.True(SpellFocusing.TryGetDamageOffset(spell, caster, target, out var offset));
                Assert.Equal(expectedOffset, offset);
            }

            target.Delete();
            var replacement = new Mobile(World.NewMobile);
            replacement.DefaultMobileInit();
            replacement.Player = false;

            try
            {
                Assert.True(SpellFocusing.TryGetDamageOffset(spell, caster, replacement, out var resetOffset));
                Assert.Equal(-30, resetOffset);
            }
            finally
            {
                replacement.Delete();
            }
        }
        finally
        {
            Core.Expansion = previousExpansion;
            item.Delete();
            target.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void SpellFocusing_UsesThePvMSequenceAndResetsAfterThePeak()
    {
        var previousExpansion = Core.Expansion;
        var caster = CreateMobile(player: true);
        var target = CreateMobile(player: false);
        var item = new Katana();
        var spell = new TestSpell(caster);

        try
        {
            Core.Expansion = Expansion.AOS;
            caster.AddItem(item);
            item.Attributes.SpellFocusing = 1;

            var expected = new[]
            {
                -30, -24, -18, -12, -6, 0, 2, 4, 6, 8, 10,
                12, 14, 16, 18, 20, 22, 24, 26, 28, 30
            };

            foreach (var expectedOffset in expected)
            {
                Assert.True(SpellFocusing.TryGetDamageOffset(spell, caster, target, out var offset));
                Assert.Equal(expectedOffset, offset);
            }

            Assert.True(SpellFocusing.TryGetDamageOffset(spell, caster, target, out var resetOffset));
            Assert.Equal(-30, resetOffset);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            item.Delete();
            target.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void SpellFocusing_HoldsThePvPCapAndResetsOnTargetChange()
    {
        var previousExpansion = Core.Expansion;
        var caster = CreateMobile(player: true);
        var target = CreateMobile(player: true);
        var secondTarget = CreateMobile(player: true);
        var item = new Katana();
        var spell = new TestSpell(caster);

        try
        {
            Core.Expansion = Expansion.AOS;
            caster.AddItem(item);
            item.Attributes.SpellFocusing = 1;

            for (var i = 0; i < 15; i++)
            {
                Assert.True(SpellFocusing.TryGetDamageOffset(spell, caster, target, out _));
            }

            for (var i = 0; i < 6; i++)
            {
                Assert.True(SpellFocusing.TryGetDamageOffset(spell, caster, target, out var offset));
                Assert.Equal(20, offset);
            }

            Assert.True(SpellFocusing.TryGetDamageOffset(spell, caster, target, out var resetOffset));
            Assert.Equal(-30, resetOffset);

            Assert.True(SpellFocusing.TryGetDamageOffset(spell, caster, secondTarget, out var targetResetOffset));
            Assert.Equal(-30, targetResetOffset);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            item.Delete();
            secondTarget.Delete();
            target.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void SpellFocusing_AppliesTheModifierAtTheSpellDamageHook()
    {
        var previousExpansion = Core.Expansion;
        var caster = CreateMobile(player: true);
        var target = CreateMobile(player: false);
        var item = new Katana();
        var spell = new TestSpell(caster);

        try
        {
            Core.Expansion = Expansion.AOS;
            caster.AddItem(item);
            item.Attributes.SpellFocusing = 1;
            var startingHits = target.Hits;

            SpellHelper.Damage(spell, target, 100, 100, 0, 0, 0, 0);

            Assert.Equal(startingHits - 70, target.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            item.Delete();
            target.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void SpellFocusing_ClearsTheSequenceWhenTheCasterIsCleared()
    {
        var previousExpansion = Core.Expansion;
        var caster = CreateMobile(player: true);
        var target = CreateMobile(player: false);
        var item = new Katana();
        var spell = new TestSpell(caster);

        try
        {
            Core.Expansion = Expansion.AOS;
            caster.AddItem(item);
            item.Attributes.SpellFocusing = 1;

            Assert.True(SpellFocusing.TryGetDamageOffset(spell, caster, target, out var firstOffset));
            Assert.Equal(-30, firstOffset);
            Assert.True(SpellFocusing.TryGetDamageOffset(spell, caster, target, out var secondOffset));
            Assert.Equal(-24, secondOffset);

            SpellFocusing.Clear(caster);

            Assert.True(SpellFocusing.TryGetDamageOffset(spell, caster, target, out var resetOffset));
            Assert.Equal(-30, resetOffset);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            item.Delete();
            target.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void SpellFocusing_RequiresAnEligibleSpellAndTheAoSExpansion()
    {
        var previousExpansion = Core.Expansion;
        var caster = CreateMobile(player: true);
        var target = CreateMobile(player: false);
        var item = new Katana();
        var qualifyingSpell = new TestSpell(caster);
        var nonQualifyingSpell = new NonQualifyingSpell(caster);

        try
        {
            caster.AddItem(item);
            item.Attributes.SpellFocusing = 1;

            Core.Expansion = Expansion.UOR;
            Assert.False(SpellFocusing.TryGetDamageOffset(qualifyingSpell, caster, target, out _));

            Core.Expansion = Expansion.AOS;
            Assert.False(SpellFocusing.TryGetDamageOffset(nonQualifyingSpell, caster, target, out _));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            item.Delete();
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

    private sealed class RecordingPropertyList : IPropertyList
    {
        public List<int> Numbers { get; } = [];

        public void Reset()
        {
        }

        public void Terminate()
        {
        }

        public void Add(int number) => Numbers.Add(number);
        public void Add(int number, string argument) => Numbers.Add(number);
        public void Add(ReadOnlySpan<char> argument)
        {
        }

        public void Add(int number, ReadOnlySpan<char> argument) => Numbers.Add(number);
        public void AddChunked(ReadOnlySpan<char> text)
        {
        }

        public OplTextBlock TextBlock() => new(this);
        public void Add(int number, int value) => Numbers.Add(number);
        public void AddLocalized(int value)
        {
        }

        public void AddLocalized(int number, int value) => Numbers.Add(number);
        public void Add(ref IPropertyList.InterpolatedStringHandler handler)
        {
        }

        public void Add(int number, ref IPropertyList.InterpolatedStringHandler handler) => Numbers.Add(number);
        public void InitializeInterpolation(int literalLength, int formattedCount)
        {
        }

        public void AppendLiteral(string value)
        {
        }

        public void AppendFormatted<T>(T value)
        {
        }

        public void AppendFormatted<T>(T value, string format)
        {
        }

        public void AppendFormatted<T>(T value, int alignment)
        {
        }

        public void AppendFormatted<T>(T value, int alignment, string format)
        {
        }

        public void AppendFormatted(ReadOnlySpan<char> value)
        {
        }

        public void AppendFormatted(ReadOnlySpan<char> value, int alignment, string format = null)
        {
        }

        public void AppendFormatted(object value, int alignment = 0, string format = null)
        {
        }

        public void AppendFormatted(string value)
        {
        }

        public void AppendFormatted(string value, int alignment, string format = null)
        {
        }
    }
}
