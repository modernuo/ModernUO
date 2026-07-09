using System;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Second;
using Server.Tests;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class CastingFocusPropertyTests
{
    private const int CastingFocusCliloc = 1113696;

    [Fact]
    public void AbsorptionAttributes_StoresDupesAndSerializesCastingFocusOnArmor()
    {
        var armor = new LeatherChest();
        var dupe = new LeatherChest();
        var deserialized = new LeatherChest();

        try
        {
            armor.AbsorptionAttributes.CastingFocus = 3;

            armor.Dupe(dupe);
            Assert.Equal(3, dupe.AbsorptionAttributes.CastingFocus);

            var writer = new BufferWriter(true);
            armor.Serialize(writer);
            var buffer = new byte[writer.Position];
            writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

            var reader = new BufferReader(buffer);
            deserialized.Deserialize(reader);

            Assert.Equal(buffer.Length, reader.Position);
            Assert.Equal(3, deserialized.AbsorptionAttributes.CastingFocus);
        }
        finally
        {
            armor.Delete();
            dupe.Delete();
            deserialized.Delete();
        }
    }

    [Fact]
    public void AbsorptionAttributes_AreStaffEditableThroughCommandProperties()
    {
        var containerProperty = typeof(BaseArmor).GetProperty(nameof(BaseArmor.AbsorptionAttributes));
        Assert.NotNull(containerProperty);
        var containerCommandProperty = containerProperty.GetCustomAttribute<CommandPropertyAttribute>();
        Assert.NotNull(containerCommandProperty);
        Assert.True(containerCommandProperty.CanModify);

        var property = typeof(AbsorptionAttributes).GetProperty(nameof(AbsorptionAttributes.CastingFocus));
        Assert.NotNull(property);
        var attribute = property.GetCustomAttribute<CommandPropertyAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(AccessLevel.GameMaster, attribute.ReadLevel);
        Assert.Equal(AccessLevel.GameMaster, attribute.WriteLevel);
    }

    [Fact]
    public void AbsorptionAttributes_GetValue_SumsEquippedArmorAndRespectsStygianAbyssGate()
    {
        var previousExpansion = Core.Expansion;
        var caster = CreateMobile(player: true);
        var chest = new LeatherChest();
        var legs = new LeatherLegs();

        try
        {
            chest.AbsorptionAttributes.CastingFocus = 5;
            legs.AbsorptionAttributes.CastingFocus = 10;
            caster.AddItem(chest);
            caster.AddItem(legs);

            Core.Expansion = Expansion.ML;
            Assert.Equal(0, AbsorptionAttributes.GetValue(caster, AbsorptionAttribute.CastingFocus));

            Core.Expansion = Expansion.SA;
            Assert.Equal(15, AbsorptionAttributes.GetValue(caster, AbsorptionAttribute.CastingFocus));
            Assert.Equal(
                AOS.CastingFocusChanceCap,
                Math.Min(
                    AOS.CastingFocusChanceCap,
                    AbsorptionAttributes.GetValue(caster, AbsorptionAttribute.CastingFocus)
                )
            );
        }
        finally
        {
            Core.Expansion = previousExpansion;
            chest.Delete();
            legs.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void BaseArmor_GetProperties_GatesCastingFocusTooltipToStygianAbyss()
    {
        var previousExpansion = Core.Expansion;
        var armor = new LeatherChest();

        try
        {
            armor.AbsorptionAttributes.CastingFocus = 3;

            Core.Expansion = Expansion.ML;
            var preStygianAbyss = new RecordingPropertyList();
            armor.GetProperties(preStygianAbyss);
            Assert.DoesNotContain(preStygianAbyss.Entries, entry => entry.Number == CastingFocusCliloc);

            Core.Expansion = Expansion.SA;
            var stygianAbyss = new RecordingPropertyList();
            armor.GetProperties(stygianAbyss);
            Assert.Contains(stygianAbyss.Entries, entry =>
                entry.Number == CastingFocusCliloc && entry.Argument == "3"
            );
        }
        finally
        {
            Core.Expansion = previousExpansion;
            armor.Delete();
        }
    }

    [Fact]
    public void OnCasterHurt_CastingFocusSuccessPreservesCastWithoutPreventingDamage()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var caster = CreateMobile(player: true);
        var armor = EquipCastingFocusArmor(caster, 1);
        var spell = SetCastingSpell(caster);

        try
        {
            Core.Expansion = Expansion.SA;
            var hits = caster.Hits;

            caster.Damage(10);

            Assert.Equal(hits - 10, caster.Hits);
            Assert.Equal(SpellState.Casting, spell.State);
            Assert.Same(spell, caster.Spell);
            Assert.Equal(0, spell.DisturbCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            armor.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void OnCasterHurt_CastingFocusFailedRollDisturbsNormally()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(99);
        var caster = CreateMobile(player: true);
        var armor = EquipCastingFocusArmor(caster, 12);
        var spell = SetCastingSpell(caster);

        try
        {
            Core.Expansion = Expansion.SA;

            caster.Damage(10);

            Assert.Equal(SpellState.None, spell.State);
            Assert.Null(caster.Spell);
            Assert.Equal(1, spell.DisturbCount);
            Assert.Equal(DisturbType.Hurt, spell.LastDisturbType);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            armor.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void OnCasterHurt_PreStygianAbyssCastingFocusDoesNotPreserveCast()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var caster = CreateMobile(player: true);
        var armor = EquipCastingFocusArmor(caster, 12);
        var spell = SetCastingSpell(caster);

        try
        {
            Core.Expansion = Expansion.ML;

            caster.Damage(10);

            Assert.Equal(SpellState.None, spell.State);
            Assert.Null(caster.Spell);
            Assert.Equal(1, spell.DisturbCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            armor.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void OnCasterHurt_ProtectionSuccessStillPreservesBeforeCastingFocus()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(99);
        var caster = CreateMobile(player: true);
        var armor = EquipCastingFocusArmor(caster, 0);
        var spell = SetCastingSpell(caster);

        try
        {
            Core.Expansion = Expansion.SA;
            ProtectionSpell.Registry[caster] = 1000;

            caster.Damage(10);

            Assert.Equal(SpellState.Casting, spell.State);
            Assert.Same(spell, caster.Spell);
            Assert.Equal(0, spell.DisturbCount);
        }
        finally
        {
            ProtectionSpell.Registry.Remove(caster);
            Core.Expansion = previousExpansion;
            armor.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void OnCasterHurt_CastingFocusCanPreserveAfterProtectionFails()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(1);
        var caster = CreateMobile(player: true);
        var armor = EquipCastingFocusArmor(caster, 12);
        var spell = SetCastingSpell(caster);

        try
        {
            Core.Expansion = Expansion.SA;
            ProtectionSpell.Registry[caster] = 0;

            caster.Damage(10);

            Assert.Equal(SpellState.Casting, spell.State);
            Assert.Same(spell, caster.Spell);
            Assert.Equal(0, spell.DisturbCount);
        }
        finally
        {
            ProtectionSpell.Registry.Remove(caster);
            Core.Expansion = previousExpansion;
            armor.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void OnCasterHurt_NonPlayerCasterRemainsUnchanged()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(99);
        var caster = CreateMobile(player: false);
        var armor = EquipCastingFocusArmor(caster, 12);
        var spell = SetCastingSpell(caster);

        try
        {
            Core.Expansion = Expansion.SA;

            caster.Damage(10);

            Assert.Equal(SpellState.Casting, spell.State);
            Assert.Same(spell, caster.Spell);
            Assert.Equal(0, spell.DisturbCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            armor.Delete();
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

    private static LeatherChest EquipCastingFocusArmor(Mobile mobile, int value)
    {
        var armor = new LeatherChest();
        armor.AbsorptionAttributes.CastingFocus = value;
        mobile.AddItem(armor);
        return armor;
    }

    private static TestSpell SetCastingSpell(Mobile caster)
    {
        var spell = new TestSpell(caster)
        {
            State = SpellState.Casting
        };

        caster.Spell = spell;
        return spell;
    }

    private sealed class TestSpell : Spell
    {
        private static readonly SpellInfo TestInfo = new("Test Spell", "test");

        public TestSpell(Mobile caster) : base(caster, null, TestInfo)
        {
        }

        public int DisturbCount { get; private set; }
        public DisturbType LastDisturbType { get; private set; }
        public override TimeSpan CastDelayBase => TimeSpan.Zero;
        public override void OnCast()
        {
        }

        public override int GetMana() => 0;

        public override void OnDisturb(DisturbType type, bool message)
        {
            DisturbCount++;
            LastDisturbType = type;
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
