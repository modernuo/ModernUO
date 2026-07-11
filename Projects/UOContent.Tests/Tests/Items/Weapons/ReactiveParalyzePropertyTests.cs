using System;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Items;
using Server.Misc;
using Server.Tests;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class ReactiveParalyzePropertyTests
{
    private const int ReactiveParalyzeCliloc = 1112364;

    [Fact]
    public void Attributes_StoreDupeAndSerializeReactiveParalyzeOnSupportedHosts()
    {
        var shield = new BronzeShield();
        var weapon = new Halberd();
        var shieldDupe = new BronzeShield();
        var weaponDupe = new Halberd();
        var deserialized = new Halberd();

        try
        {
            shield.ArmorAttributes.ReactiveParalyze = 1;
            weapon.WeaponAttributes.ReactiveParalyze = 1;

            shield.Dupe(shieldDupe);
            weapon.Dupe(weaponDupe);

            var writer = new BufferWriter(true);
            weapon.Serialize(writer);
            var buffer = new byte[writer.Position];
            writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);
            var reader = new BufferReader(buffer);
            deserialized.Deserialize(reader);

            Assert.Equal(1, shieldDupe.ArmorAttributes.ReactiveParalyze);
            Assert.Equal(1, weaponDupe.WeaponAttributes.ReactiveParalyze);
            Assert.Equal(buffer.Length, reader.Position);
            Assert.Equal(1, deserialized.WeaponAttributes.ReactiveParalyze);
        }
        finally
        {
            shield.Delete();
            weapon.Delete();
            shieldDupe.Delete();
            weaponDupe.Delete();
            deserialized.Delete();
        }
    }

    [Fact]
    public void Attributes_ExposeStaffEditableReactiveParalyzeWrappers()
    {
        AssertStaffEditable(typeof(AosArmorAttributes), nameof(AosArmorAttributes.ReactiveParalyze));
        AssertStaffEditable(typeof(AosWeaponAttributes), nameof(AosWeaponAttributes.ReactiveParalyze));
    }

    [Fact]
    public void Attributes_NormalizeReactiveParalyzeToBinaryPresence()
    {
        var shield = new BronzeShield();
        var weapon = new Halberd();

        try
        {
            shield.ArmorAttributes.ReactiveParalyze = -1;
            weapon.WeaponAttributes.ReactiveParalyze = 2;

            Assert.Equal(1, shield.ArmorAttributes.ReactiveParalyze);
            Assert.Equal(1, weapon.WeaponAttributes.ReactiveParalyze);

            shield.ArmorAttributes.ReactiveParalyze = 0;
            weapon.WeaponAttributes.ReactiveParalyze = 0;

            Assert.Equal(0, shield.ArmorAttributes.ReactiveParalyze);
            Assert.Equal(0, weapon.WeaponAttributes.ReactiveParalyze);
        }
        finally
        {
            shield.Delete();
            weapon.Delete();
        }
    }

    [Fact]
    public void GetProperties_GatesReactiveParalyzeToStygianAbyssAndSupportedHosts()
    {
        var previousExpansion = Core.Expansion;
        var shield = new BronzeShield();
        var weapon = new Halberd();
        var oneHanded = new Katana();
        var ranged = new Bow();
        var armor = new LeatherChest();

        try
        {
            shield.ArmorAttributes.ReactiveParalyze = 1;
            weapon.WeaponAttributes.ReactiveParalyze = 1;
            oneHanded.WeaponAttributes[AosWeaponAttribute.ReactiveParalyze] = 1;
            ranged.WeaponAttributes[AosWeaponAttribute.ReactiveParalyze] = 1;
            armor.ArmorAttributes[AosArmorAttribute.ReactiveParalyze] = 1;

            Core.Expansion = Expansion.ML;
            AssertDoesNotShowReactiveParalyze(shield);
            AssertDoesNotShowReactiveParalyze(weapon);

            Core.Expansion = Expansion.SA;
            AssertShowsReactiveParalyze(shield);
            AssertShowsReactiveParalyze(weapon);
            AssertDoesNotShowReactiveParalyze(oneHanded);
            AssertDoesNotShowReactiveParalyze(ranged);
            AssertDoesNotShowReactiveParalyze(armor);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            shield.Delete();
            weapon.Delete();
            oneHanded.Delete();
            ranged.Delete();
            armor.Delete();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SuccessfulParry_ParalyzesAttackerFromShieldOrTwoHandedWeapon(bool shieldHost)
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var attacker = CreateMobile(player: true, magicResist: 90.0);
        var defender = CreateDefender();
        var attackingWeapon = new Katana();
        Item reactiveHost = shieldHost ? new BronzeShield { Layer = Layer.TwoHanded } : new Halberd();

        try
        {
            EnsureSkillChecksConfigured();
            Core.Expansion = Expansion.SA;

            if (reactiveHost is BaseShield shield)
            {
                shield.ArmorAttributes.ReactiveParalyze = 1;
            }
            else
            {
                ((BaseWeapon)reactiveHost).WeaponAttributes.ReactiveParalyze = 1;
            }

            defender.AddItem(reactiveHost);

            Assert.Equal(0, attackingWeapon.AbsorbDamageAOS(attacker, defender, 10));
            Assert.True(attacker.Paralyzed);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            attackingWeapon.Delete();
            reactiveHost.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void ReactiveParalyze_FailedRollPreSaAndAlreadyParalyzedAttackerDoNotApplyOrRefreshControl()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile(player: true, magicResist: 90.0);
        var defender = CreateDefender();
        var shield = new BronzeShield { Layer = Layer.TwoHanded };

        try
        {
            shield.ArmorAttributes.ReactiveParalyze = 1;
            defender.AddItem(shield);

            Core.Expansion = Expansion.SA;
            using (var failedRoll = new PredictableRandom(99))
            {
                BaseWeapon.TryApplyReactiveParalyze(attacker, defender);
            }

            Assert.False(attacker.Paralyzed);

            Core.Expansion = Expansion.ML;
            using (var successfulRoll = new PredictableRandom(0))
            {
                BaseWeapon.TryApplyReactiveParalyze(attacker, defender);
            }

            Assert.False(attacker.Paralyzed);

            Core.Expansion = Expansion.SA;
            attacker.Paralyze(TimeSpan.FromSeconds(10));
            using (var successfulRoll = new PredictableRandom(0))
            {
                BaseWeapon.TryApplyReactiveParalyze(attacker, defender);
            }

            Assert.True(attacker.Paralyzed);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            shield.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void ReactiveParalyze_UsesCurrentMageryParalyzeDurationPolicyForPlayersAndCreatures()
    {
        var playerAttacker = CreateMobile(player: true, magicResist: 90.0);
        var creatureAttacker = CreateMobile(player: false, magicResist: 90.0);
        var defender = CreateDefender(magery: 100.0);

        try
        {
            Assert.Equal(TimeSpan.FromSeconds(1), BaseWeapon.GetReactiveParalyzeDuration(defender, playerAttacker));
            Assert.Equal(TimeSpan.FromSeconds(3), BaseWeapon.GetReactiveParalyzeDuration(defender, creatureAttacker));
        }
        finally
        {
            playerAttacker.Delete();
            creatureAttacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void RunicAttributeGeneration_DoesNotRollReactiveParalyze()
    {
        var previousExpansion = Core.Expansion;
        var shield = new BronzeShield();
        var weapon = new Halberd();

        try
        {
            Core.Expansion = Expansion.SA;

            BaseRunicTool.ApplyAttributesTo(shield, false, 0, 25, 100, 100);
            BaseRunicTool.ApplyAttributesTo(weapon, false, 0, 25, 100, 100);

            Assert.Equal(0, shield.ArmorAttributes.ReactiveParalyze);
            Assert.Equal(0, weapon.WeaponAttributes.ReactiveParalyze);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            shield.Delete();
            weapon.Delete();
        }
    }

    private static Mobile CreateDefender(double magery = 100.0)
    {
        var defender = CreateMobile(player: true);
        defender.SkillsCap = 7200;
        defender.Skills[SkillName.Parry].Cap = 120.0;
        defender.Skills[SkillName.Parry].Base = 120.0;
        defender.Skills[SkillName.Magery].Base = magery;
        return defender;
    }

    private static Mobile CreateMobile(bool player, double magicResist = 0.0)
    {
        var mobile = new Mobile(World.NewMobile) { Player = player };
        mobile.DefaultMobileInit();
        mobile.InitStats(100, 100, 100);
        mobile.Skills[SkillName.MagicResist].Base = magicResist;
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

    private static void AssertStaffEditable(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        var attribute = property?.GetCustomAttribute<CommandPropertyAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(AccessLevel.GameMaster, attribute.ReadLevel);
        Assert.Equal(AccessLevel.GameMaster, attribute.WriteLevel);
    }

    private static void AssertShowsReactiveParalyze(Item item)
    {
        var properties = new RecordingPropertyList();
        item.GetProperties(properties);

        Assert.Contains(properties.Entries, entry => entry.Number == ReactiveParalyzeCliloc);
    }

    private static void AssertDoesNotShowReactiveParalyze(Item item)
    {
        var properties = new RecordingPropertyList();
        item.GetProperties(properties);

        Assert.DoesNotContain(properties.Entries, entry => entry.Number == ReactiveParalyzeCliloc);
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
