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
public class ResonancePropertyTests
{
    private const int FireResonanceCliloc = 1154655;
    private const int ColdResonanceCliloc = 1154656;
    private const int PoisonResonanceCliloc = 1154657;
    private const int EnergyResonanceCliloc = 1154658;
    private const int KineticResonanceCliloc = 1154659;

    [Fact]
    public void AbsorptionAttributes_StoresDupesAndSerializesResonanceOnValidHostsOnly()
    {
        var shield = new BronzeShield { Layer = Layer.TwoHanded };
        var shieldDupe = new BronzeShield { Layer = Layer.TwoHanded };
        var shieldDeserialized = new BronzeShield { Layer = Layer.TwoHanded };
        var weapon = new WarHammer();
        var weaponDupe = new WarHammer();
        var deserialized = new WarHammer();
        var armor = new LeatherChest();
        var oneHandedWeapon = new Katana();

        try
        {
            SetAllResonance(shield.AbsorptionAttributes, 1);
            SetAllResonance(weapon.AbsorptionAttributes, 6);
            SetAllResonance(armor.AbsorptionAttributes, 11);
            SetAllResonance(oneHandedWeapon.AbsorptionAttributes, 16);

            shield.Dupe(shieldDupe);
            weapon.Dupe(weaponDupe);

            AssertResonance(shieldDupe.AbsorptionAttributes, 1);
            AssertResonance(weaponDupe.AbsorptionAttributes, 6);
            AssertResonance(armor.AbsorptionAttributes, 0);
            AssertResonance(oneHandedWeapon.AbsorptionAttributes, 0);

            var shieldWriter = new BufferWriter(true);
            shield.Serialize(shieldWriter);
            var shieldBuffer = new byte[shieldWriter.Position];
            shieldWriter.Buffer.AsSpan(0, (int)shieldWriter.Position).CopyTo(shieldBuffer);

            var shieldReader = new BufferReader(shieldBuffer);
            shieldDeserialized.Deserialize(shieldReader);

            Assert.Equal(shieldBuffer.Length, shieldReader.Position);
            AssertResonance(shieldDeserialized.AbsorptionAttributes, 1);

            var writer = new BufferWriter(true);
            weapon.Serialize(writer);
            var buffer = new byte[writer.Position];
            writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

            var reader = new BufferReader(buffer);
            deserialized.Deserialize(reader);

            Assert.Equal(buffer.Length, reader.Position);
            AssertResonance(deserialized.AbsorptionAttributes, 6);
        }
        finally
        {
            shield.Delete();
            shieldDupe.Delete();
            shieldDeserialized.Delete();
            weapon.Delete();
            weaponDupe.Delete();
            deserialized.Delete();
            armor.Delete();
            oneHandedWeapon.Delete();
        }
    }

    [Fact]
    public void AbsorptionAttributes_ResonancePropertiesAreStaffEditable()
    {
        var weaponContainer = typeof(BaseWeapon).GetProperty(nameof(BaseWeapon.AbsorptionAttributes));
        Assert.NotNull(weaponContainer);
        Assert.True(weaponContainer.GetCustomAttribute<CommandPropertyAttribute>().CanModify);

        var properties = new[]
        {
            nameof(AbsorptionAttributes.FireResonance),
            nameof(AbsorptionAttributes.ColdResonance),
            nameof(AbsorptionAttributes.PoisonResonance),
            nameof(AbsorptionAttributes.EnergyResonance),
            nameof(AbsorptionAttributes.KineticResonance)
        };

        foreach (var name in properties)
        {
            var property = typeof(AbsorptionAttributes).GetProperty(name);
            Assert.NotNull(property);

            var attribute = property.GetCustomAttribute<CommandPropertyAttribute>();
            Assert.NotNull(attribute);
            Assert.Equal(AccessLevel.GameMaster, attribute.ReadLevel);
            Assert.Equal(AccessLevel.GameMaster, attribute.WriteLevel);
        }
    }

    [Fact]
    public void ResonanceTooltips_AreVisibleOnlyAtStygianAbyssOnShieldsAndTwoHandedWeapons()
    {
        var previousExpansion = Core.Expansion;
        var shield = new BronzeShield { Layer = Layer.TwoHanded };
        var weapon = new WarHammer();
        var armor = new LeatherChest();

        try
        {
            SetAllResonance(shield.AbsorptionAttributes, 1);
            SetAllResonance(weapon.AbsorptionAttributes, 6);
            SetAllResonance(armor.AbsorptionAttributes, 11);

            Core.Expansion = Expansion.ML;
            AssertNoResonanceTooltips(shield);
            AssertNoResonanceTooltips(weapon);

            Core.Expansion = Expansion.SA;
            AssertResonanceTooltips(shield, 1);
            AssertResonanceTooltips(weapon, 6);
            AssertNoResonanceTooltips(armor);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            shield.Delete();
            weapon.Delete();
            armor.Delete();
        }
    }

    [Fact]
    public void ResonanceAggregation_UsesOnlyMatchingEligibleEquipmentAndCapsAtFortyPercent()
    {
        var previousExpansion = Core.Expansion;
        var mobile = CreateMobile(player: true);
        var shield = new BronzeShield { Layer = Layer.TwoHanded };
        var weapon = new WarHammer();
        var oneHandedWeapon = new Katana();

        try
        {
            shield.AbsorptionAttributes.FireResonance = 25;
            shield.AbsorptionAttributes.ColdResonance = 16;
            weapon.AbsorptionAttributes.FireResonance = 25;
            weapon.AbsorptionAttributes.PoisonResonance = 19;
            oneHandedWeapon.AbsorptionAttributes.FireResonance = 99;

            mobile.AddItem(shield);
            mobile.AddItem(weapon);
            mobile.AddItem(oneHandedWeapon);

            Assert.Contains(shield, mobile.Items);
            Assert.Contains(weapon, mobile.Items);
            Assert.Contains(oneHandedWeapon, mobile.Items);
            Assert.Equal(Layer.TwoHanded, shield.Layer);
            Assert.Equal(Layer.TwoHanded, weapon.Layer);

            Core.Expansion = Expansion.ML;
            Assert.Equal(0, AbsorptionAttributes.GetResonanceValue(mobile, DamageType.Fire));

            Core.Expansion = Expansion.SA;
            Assert.Equal(AOS.ResonanceChanceCap, AbsorptionAttributes.GetResonanceValue(mobile, DamageType.Fire));
            Assert.Equal(16, AbsorptionAttributes.GetResonanceValue(mobile, DamageType.Cold));
            Assert.Equal(19, AbsorptionAttributes.GetResonanceValue(mobile, DamageType.Poison));
            Assert.Equal(0, AbsorptionAttributes.GetResonanceValue(mobile, DamageType.Energy));
            Assert.Equal(0, oneHandedWeapon.AbsorptionAttributes.FireResonance);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            shield.Delete();
            weapon.Delete();
            oneHandedWeapon.Delete();
            mobile.Delete();
        }
    }

    [Theory]
    [InlineData(DamageType.Physical)]
    [InlineData(DamageType.Fire)]
    [InlineData(DamageType.Cold)]
    [InlineData(DamageType.Poison)]
    [InlineData(DamageType.Energy)]
    public void TypedDamage_MatchingResonancePreservesCastWithoutChangingDamage(DamageType damageType)
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var caster = CreateMobile(player: true);
        var source = CreateMobile(player: true);
        var weapon = new WarHammer();

        try
        {
            Core.Expansion = Expansion.SA;
            SetResonance(weapon.AbsorptionAttributes, damageType, 1);
            caster.AddItem(weapon);
            var spell = SetCastingSpell(caster);
            var hits = caster.Hits;

            ApplyTypedDamage(caster, source, damageType);

            Assert.Equal(hits - 10, caster.Hits);
            Assert.Equal(SpellState.Casting, spell.State);
            Assert.Same(spell, caster.Spell);
            Assert.Equal(0, spell.DisturbCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            caster.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void NonMatchingAndUntypedDamage_DisturbsNormallyAndTypedContextDoesNotLeak()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var caster = CreateMobile(player: true);
        var source = CreateMobile(player: true);
        var weapon = new WarHammer();

        try
        {
            Core.Expansion = Expansion.SA;
            weapon.AbsorptionAttributes.FireResonance = 1;
            caster.AddItem(weapon);

            AssertDisturbsAfter(() => ApplyDamage(caster, source, physical: 50, fire: 50));
            AssertDisturbsAfter(() => ApplyDamage(caster, source, fire: 100, chaos: 1));
            AssertDisturbsAfter(() => ApplyDamage(caster, source, fire: 100, direct: 1));
            AssertDisturbsAfter(() => caster.Damage(10));

            var preservedSpell = SetCastingSpell(caster);
            ApplyDamage(caster, source, fire: 100);
            Assert.Equal(SpellState.Casting, preservedSpell.State);

            AssertDisturbsAfter(() => caster.Damage(10));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            caster.Delete();
            source.Delete();
        }

        void AssertDisturbsAfter(Action action)
        {
            var spell = SetCastingSpell(caster);
            action();
            Assert.Equal(SpellState.None, spell.State);
            Assert.Null(caster.Spell);
            Assert.Equal(1, spell.DisturbCount);
            Assert.Equal(DisturbType.Hurt, spell.LastDisturbType);
        }
    }

    [Fact]
    public void HurtPreservation_OrderIsProtectionThenCastingFocusThenResonance()
    {
        var previousExpansion = Core.Expansion;
        var caster = CreateMobile(player: true);
        var source = CreateMobile(player: true);
        var weapon = new WarHammer();
        var armor = new LeatherChest();

        try
        {
            Core.Expansion = Expansion.SA;
            weapon.AbsorptionAttributes.FireResonance = 1;
            caster.AddItem(weapon);
            caster.AddItem(armor);

            using (new PredictableRandom(0))
            {
                ProtectionSpell.Registry[caster] = 1000;
                var protectionSpell = SetCastingSpell(caster);
                ApplyDamage(caster, source, fire: 100);
                Assert.Equal(SpellState.Casting, protectionSpell.State);
                Assert.Equal(0, protectionSpell.DisturbCount);
            }

            ProtectionSpell.Registry[caster] = -1;
            armor.AbsorptionAttributes.CastingFocus = 1;
            using (new PredictableRandom(0))
            {
                var focusSpell = SetCastingSpell(caster);
                ApplyDamage(caster, source, fire: 100);
                Assert.Equal(SpellState.Casting, focusSpell.State);
                Assert.Equal(0, focusSpell.DisturbCount);
            }

            armor.AbsorptionAttributes.CastingFocus = 0;
            using (new PredictableRandom(0))
            {
                var resonanceSpell = SetCastingSpell(caster);
                ApplyDamage(caster, source, fire: 100);
                Assert.Equal(SpellState.Casting, resonanceSpell.State);
                Assert.Equal(0, resonanceSpell.DisturbCount);
            }

            ProtectionSpell.Registry.Remove(caster);
            using (new PredictableRandom(99))
            {
                var failedSpell = SetCastingSpell(caster);
                ApplyDamage(caster, source, fire: 100);
                Assert.Equal(SpellState.None, failedSpell.State);
                Assert.Equal(1, failedSpell.DisturbCount);
            }
        }
        finally
        {
            ProtectionSpell.Registry.Remove(caster);
            Core.Expansion = previousExpansion;
            weapon.Delete();
            armor.Delete();
            caster.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void Resonance_DoesNotChangeNonPlayerOrNonHurtDisturbanceBehavior()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var creature = CreateMobile(player: false);
        var weapon = new WarHammer();

        try
        {
            Core.Expansion = Expansion.SA;
            weapon.AbsorptionAttributes.FireResonance = 40;
            creature.AddItem(weapon);
            var spell = SetCastingSpell(creature);

            ApplyDamage(creature, null, fire: 100);
            Assert.Equal(SpellState.Casting, spell.State);
            Assert.Equal(0, spell.DisturbCount);

            creature.Player = true;
            spell.Disturb(DisturbType.EquipRequest, false, true);
            Assert.Equal(SpellState.None, spell.State);
            Assert.Equal(1, spell.DisturbCount);
            Assert.Equal(DisturbType.EquipRequest, spell.LastDisturbType);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            creature.Delete();
        }
    }

    private static void SetAllResonance(AbsorptionAttributes attributes, int value)
    {
        attributes.FireResonance = value;
        attributes.ColdResonance = value + 1;
        attributes.PoisonResonance = value + 2;
        attributes.EnergyResonance = value + 3;
        attributes.KineticResonance = value + 4;
    }

    private static void AssertResonance(AbsorptionAttributes attributes, int value)
    {
        Assert.Equal(value, attributes.FireResonance);
        Assert.Equal(value + (value == 0 ? 0 : 1), attributes.ColdResonance);
        Assert.Equal(value + (value == 0 ? 0 : 2), attributes.PoisonResonance);
        Assert.Equal(value + (value == 0 ? 0 : 3), attributes.EnergyResonance);
        Assert.Equal(value + (value == 0 ? 0 : 4), attributes.KineticResonance);
    }

    private static void SetResonance(AbsorptionAttributes attributes, DamageType damageType, int value)
    {
        switch (damageType)
        {
            case DamageType.Physical:
                attributes.KineticResonance = value;
                break;
            case DamageType.Fire:
                attributes.FireResonance = value;
                break;
            case DamageType.Cold:
                attributes.ColdResonance = value;
                break;
            case DamageType.Poison:
                attributes.PoisonResonance = value;
                break;
            case DamageType.Energy:
                attributes.EnergyResonance = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(damageType));
        }
    }

    private static void AssertNoResonanceTooltips(Item item)
    {
        var list = new RecordingPropertyList();
        item.GetProperties(list);
        Assert.DoesNotContain(list.Entries, entry => IsResonanceCliloc(entry.Number));
    }

    private static void AssertResonanceTooltips(Item item, int firstValue)
    {
        var list = new RecordingPropertyList();
        item.GetProperties(list);
        Assert.Contains(list.Entries, entry => entry.Number == FireResonanceCliloc && entry.Argument == firstValue.ToString());
        Assert.Contains(list.Entries, entry => entry.Number == ColdResonanceCliloc && entry.Argument == (firstValue + 1).ToString());
        Assert.Contains(list.Entries, entry => entry.Number == PoisonResonanceCliloc && entry.Argument == (firstValue + 2).ToString());
        Assert.Contains(list.Entries, entry => entry.Number == EnergyResonanceCliloc && entry.Argument == (firstValue + 3).ToString());
        Assert.Contains(list.Entries, entry => entry.Number == KineticResonanceCliloc && entry.Argument == (firstValue + 4).ToString());
    }

    private static bool IsResonanceCliloc(int number) => number is FireResonanceCliloc or ColdResonanceCliloc or PoisonResonanceCliloc or EnergyResonanceCliloc or KineticResonanceCliloc;

    private static void ApplyTypedDamage(Mobile defender, Mobile source, DamageType type)
    {
        switch (type)
        {
            case DamageType.Physical:
                ApplyDamage(defender, source, physical: 100);
                break;
            case DamageType.Fire:
                ApplyDamage(defender, source, fire: 100);
                break;
            case DamageType.Cold:
                ApplyDamage(defender, source, cold: 100);
                break;
            case DamageType.Poison:
                ApplyDamage(defender, source, poison: 100);
                break;
            case DamageType.Energy:
                ApplyDamage(defender, source, energy: 100);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    private static void ApplyDamage(
        Mobile defender,
        Mobile source,
        int physical = 0,
        int fire = 0,
        int cold = 0,
        int poison = 0,
        int energy = 0,
        int chaos = 0,
        int direct = 0
    ) => AOS.Damage(defender, source, 10, true, physical, fire, cold, poison, energy, chaos, direct);

    private static Mobile CreateMobile(bool player)
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.Player = player;
        mobile.RawStr = 100;
        mobile.Hits = mobile.HitsMax;
        mobile.MoveToWorld(new Point3D(6200, 500, 0), Map.Felucca);
        return mobile;
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
        public void Add(ref IPropertyList.InterpolatedStringHandler handler) => Entries.Add(new PropertyEntry(0, _interpolated));
        public void Add(int number, ref IPropertyList.InterpolatedStringHandler handler) => Entries.Add(new PropertyEntry(number, _interpolated));
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
