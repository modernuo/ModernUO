using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class FocusPropertyTests
{
    private const int FocusCliloc = 1150018;
    private static readonly Point3D TestLocation = new(6200, 500, 0);

    public FocusPropertyTests()
    {
        FocusContext.Configure();
    }

    [Fact]
    public void ExtendedWeaponAttributes_StoresAndDupesFocus()
    {
        var weapon = new TestKatana();
        var dupe = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.Focus = 1;
            weapon.Dupe(dupe);

            Assert.Equal(1, weapon.ExtendedWeaponAttributes.Focus);
            Assert.Equal(1, dupe.ExtendedWeaponAttributes.Focus);
        }
        finally
        {
            weapon.Delete();
            dupe.Delete();
        }
    }

    [Fact]
    public void FocusContext_IsTransientAndIsNotRestoredByWeaponSerialization()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 500, 0));
        var weapon = EquipFocusWeapon(attacker);
        var deserialized = new TestKatana();

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.OnHit(attacker, defender);
            Assert.Equal(1, FocusContext.ActiveContextCount);

            var writer = new BufferWriter(true);
            weapon.Serialize(writer);
            var buffer = new byte[writer.Position];
            writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

            deserialized.Deserialize(new BufferReader(buffer));

            Assert.Equal(1, deserialized.ExtendedWeaponAttributes.Focus);
            Assert.Equal(0, FocusContext.GetCurrentModifierForTests(deserialized));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            FocusContext.ClearAll();
            weapon.Delete();
            deserialized.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void ExtendedWeaponAttributes_GetProperties_GatesFocusTooltipToHighSeas()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.Focus = 1;

            Core.Expansion = Expansion.HS - 1;
            var preHighSeas = new RecordingPropertyList();
            weapon.ExtendedWeaponAttributes.GetProperties(preHighSeas);
            Assert.DoesNotContain(preHighSeas.Numbers, number => number == FocusCliloc);

            Core.Expansion = Expansion.HS;
            var highSeas = new RecordingPropertyList();
            weapon.ExtendedWeaponAttributes.GetProperties(highSeas);
            Assert.Contains(highSeas.Numbers, number => number == FocusCliloc);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnHit_UsesTheSameFocusSequenceForPvmAndPvp(bool playerTarget)
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(
            player: playerTarget,
            location: new Point3D(6201, 500, 0),
            hitsMax: 5000,
            hits: 5000
        );
        var weapon = EquipFocusWeapon(attacker);

        try
        {
            Core.Expansion = Expansion.HS;
            var expectedDamage = new[] { 50, 60, 70, 80, 90, 100, 110, 120, 120 };

            foreach (var expected in expectedDamage)
            {
                var before = defender.Hits;
                weapon.OnHit(attacker, defender);
                Assert.Equal(expected, before - defender.Hits);
            }

            Assert.Equal(FocusContext.MaximumDamageModifier, FocusContext.GetCurrentModifierForTests(weapon));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            FocusContext.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_ChangingTargetResetsToTheNegativeStartingModifier()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var firstTarget = CreateMobile(location: new Point3D(6201, 500, 0));
        var secondTarget = CreateMobile(location: new Point3D(6202, 500, 0));
        var weapon = EquipFocusWeapon(attacker);

        try
        {
            Core.Expansion = Expansion.HS;

            var firstBefore = firstTarget.Hits;
            weapon.OnHit(attacker, firstTarget);
            Assert.Equal(50, firstBefore - firstTarget.Hits);

            firstBefore = firstTarget.Hits;
            weapon.OnHit(attacker, firstTarget);
            Assert.Equal(60, firstBefore - firstTarget.Hits);

            var secondBefore = secondTarget.Hits;
            weapon.OnHit(attacker, secondTarget);
            Assert.Equal(50, secondBefore - secondTarget.Hits);
            Assert.Equal(-40, FocusContext.GetCurrentModifierForTests(weapon));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            FocusContext.ClearAll();
            weapon.Delete();
            attacker.Delete();
            firstTarget.Delete();
            secondTarget.Delete();
        }
    }

    [Fact]
    public void MissesParriesAndZeroDamageHitsDoNotAdvanceFocus()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 500, 0));
        var weapon = EquipFocusWeapon(attacker);

        try
        {
            Core.Expansion = Expansion.HS;

            var before = defender.Hits;
            weapon.OnHit(attacker, defender);
            Assert.Equal(50, before - defender.Hits);

            weapon.OnMiss(attacker, defender);
            before = defender.Hits;
            weapon.OnHit(attacker, defender);
            Assert.Equal(60, before - defender.Hits);

            weapon.AbsorbAllDamage = true;
            WeaponAbility.Table[attacker] = new TestWeaponAbility();
            before = defender.Hits;
            weapon.OnHit(attacker, defender);
            Assert.Equal(before, defender.Hits);

            WeaponAbility.Table.Remove(attacker);
            before = defender.Hits;
            weapon.OnHit(attacker, defender);
            Assert.Equal(before, defender.Hits);

            weapon.AbsorbAllDamage = false;
            before = defender.Hits;
            weapon.OnHit(attacker, defender);
            Assert.Equal(70, before - defender.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            WeaponAbility.Table.Remove(attacker);
            FocusContext.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_CombinesFocusOnceWithAnExistingWeaponDamageModifier()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 500, 0));
        var weapon = EquipFocusWeapon(attacker);

        try
        {
            Core.Expansion = Expansion.HS;

            var before = defender.Hits;
            weapon.OnHit(attacker, defender, 2.0);

            // Existing +100% bonus and Focus -50% are combined once in the shared percentage stage.
            Assert.Equal(150, before - defender.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            FocusContext.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_PreHighSeas_DoesNotApplyOrCreateFocusState()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 500, 0));
        var weapon = EquipFocusWeapon(attacker);

        try
        {
            Core.Expansion = Expansion.SA;
            var before = defender.Hits;
            weapon.OnHit(attacker, defender);

            Assert.Equal(100, before - defender.Hits);
            Assert.Equal(0, FocusContext.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            FocusContext.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_PlayerTargetsUseTheSameSequenceAndResetOnTargetSwitch()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var firstTarget = CreatePlayerMobile(new Point3D(6201, 500, 0));
        var secondTarget = CreatePlayerMobile(new Point3D(6202, 500, 0));
        var weapon = EquipFocusWeapon(attacker);

        try
        {
            Core.Expansion = Expansion.HS;

            var before = firstTarget.Hits;
            weapon.OnHit(attacker, firstTarget);
            Assert.Equal(50, before - firstTarget.Hits);

            before = firstTarget.Hits;
            weapon.OnHit(attacker, firstTarget);
            Assert.Equal(60, before - firstTarget.Hits);

            before = secondTarget.Hits;
            weapon.OnHit(attacker, secondTarget);
            Assert.Equal(50, before - secondTarget.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            FocusContext.ClearAll();
            weapon.Delete();
            attacker.Delete();
            firstTarget.Delete();
            secondTarget.Delete();
        }
    }

    [Fact]
    public void FocusContext_ClearsWhenWeaponIsUnequippedDeletedOrPropertyRemoved()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 500, 0));
        var weapon = EquipFocusWeapon(attacker);

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.OnHit(attacker, defender);
            Assert.Equal(1, FocusContext.ActiveContextCount);

            attacker.RemoveItem(weapon);
            Assert.Equal(0, FocusContext.ActiveContextCount);

            attacker.AddItem(weapon);
            weapon.OnHit(attacker, defender);
            Assert.Equal(1, FocusContext.ActiveContextCount);

            weapon.ExtendedWeaponAttributes.Focus = 0;
            Assert.Equal(0, FocusContext.ActiveContextCount);

            weapon.ExtendedWeaponAttributes.Focus = 1;
            attacker.AddItem(weapon);
            weapon.OnHit(attacker, defender);
            Assert.Equal(1, FocusContext.ActiveContextCount);

            weapon.Delete();
            Assert.Equal(0, FocusContext.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            FocusContext.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void FocusContext_ClearsWhenTargetDiesAndWhenAttackerLogsOutOrDies()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreatePlayerMobile();
        var target = CreatePlayerMobile(new Point3D(6201, 500, 0));
        var deletedTarget = CreatePlayerMobile(new Point3D(6202, 500, 0));
        var secondTarget = CreateMobile(location: new Point3D(6203, 500, 0));
        var weapon = EquipFocusWeapon(attacker);

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.OnHit(attacker, target);
            Assert.Equal(1, FocusContext.ActiveContextCount);

            PlayerMobile.PlayerDeathEvent(target);
            Assert.Equal(0, FocusContext.ActiveContextCount);

            weapon.OnHit(attacker, deletedTarget);
            Assert.Equal(1, FocusContext.ActiveContextCount);

            deletedTarget.Delete();
            Assert.Equal(0, FocusContext.ActiveContextCount);

            weapon.OnHit(attacker, secondTarget);
            Assert.Equal(1, FocusContext.ActiveContextCount);

            EventSink.InvokeLogout(attacker);
            Assert.Equal(0, FocusContext.ActiveContextCount);

            weapon.OnHit(attacker, secondTarget);
            Assert.Equal(1, FocusContext.ActiveContextCount);

            PlayerMobile.PlayerDeathEvent(attacker);
            Assert.Equal(0, FocusContext.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            FocusContext.ClearAll();
            weapon.Delete();
            attacker.Delete();
            target.Delete();
            deletedTarget.Delete();
            secondTarget.Delete();
        }
    }

    [Fact]
    public void FocusContext_ClearsOnRealCreatureDeathAndDeletion()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateCreature(new Point3D(6200, 500, 0));
        var target = CreateCreature(new Point3D(6201, 500, 0));
        var deletedTarget = CreateCreature(new Point3D(6202, 500, 0));
        var replacementTarget = CreateCreature(new Point3D(6203, 500, 0));
        var weapon = EquipFocusWeapon(attacker);

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.OnHit(attacker, target);
            Assert.Equal(1, FocusContext.ActiveContextCount);

            target.Kill();
            Assert.Equal(0, FocusContext.ActiveContextCount);

            weapon.OnHit(attacker, deletedTarget);
            Assert.Equal(1, FocusContext.ActiveContextCount);

            deletedTarget.Delete();
            Assert.Equal(0, FocusContext.ActiveContextCount);

            weapon.OnHit(attacker, replacementTarget);
            Assert.Equal(1, FocusContext.ActiveContextCount);

            attacker.Kill();
            Assert.Equal(0, FocusContext.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            FocusContext.ClearAll();
            weapon.Delete();
            attacker.Delete();
            target.Delete();
            deletedTarget.Delete();
            replacementTarget.Delete();
        }
    }

    [Fact]
    public void RunicAttributeGeneration_DoesNotRollFocus()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            Core.Expansion = Expansion.HS;
            BaseRunicTool.ApplyAttributesTo(weapon, false, 0, 25, 100, 100);

            Assert.Equal(0, weapon.ExtendedWeaponAttributes.Focus);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    private static TestKatana EquipFocusWeapon(Mobile wielder)
    {
        var weapon = new TestKatana
        {
            Layer = Layer.OneHanded
        };

        weapon.ExtendedWeaponAttributes.Focus = 1;
        wielder.AddItem(weapon);
        Assert.Same(weapon, wielder.Weapon);
        return weapon;
    }

    private static Mobile CreateMobile(
        bool player = false,
        Point3D location = default,
        int hitsMax = 500,
        int hits = 500
    )
    {
        var mobile = new Mobile(World.NewMobile)
        {
            Player = player
        };

        mobile.DefaultMobileInit();
        InitializeHits(mobile, hitsMax, hits);
        mobile.MoveToWorld(location == default ? TestLocation : location, Map.Felucca);
        return mobile;
    }

    private static PlayerMobile CreatePlayerMobile(Point3D location = default)
    {
        var mobile = new PlayerMobile(World.NewMobile);

        mobile.DefaultMobileInit();
        mobile.RawStr = 200;
        mobile.Player = true;
        mobile.Hits = mobile.HitsMax;
        mobile.MoveToWorld(location == default ? TestLocation : location, Map.Felucca);
        return mobile;
    }

    private static TestCreature CreateCreature(Point3D location)
    {
        var creature = new TestCreature();
        creature.RawStr = 200;
        creature.Hits = creature.HitsMax;
        creature.MoveToWorld(location, Map.Felucca);
        return creature;
    }

    private static void InitializeHits(Mobile mobile, int hitsMax, int hits)
    {
        mobile.RawStr = Math.Max(1, (hitsMax - 50) * 2);
        Assert.Equal(hitsMax, mobile.HitsMax);
        mobile.Hits = hits;
    }

    private sealed class TestKatana : Katana
    {
        public bool AbsorbAllDamage { get; set; }

        public override int ComputeDamage(Mobile attacker, Mobile defender) => 100;

        public override int AbsorbDamage(Mobile attacker, Mobile defender, int damage) => AbsorbAllDamage ? 0 : damage;

        public override void AddBlood(Mobile attacker, Mobile defender, int damage)
        {
        }
    }

    private sealed class TestWeaponAbility : WeaponAbility
    {
    }

    private sealed class TestCreature : BaseCreature
    {
        public TestCreature() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
        {
            Body = 0xC8;
        }

        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.2;
            passiveSpeed = 0.4;
        }
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
