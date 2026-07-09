using System;
using System.Collections.Generic;
using System.Globalization;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Bushido;
using Server.Spells.Ninjitsu;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class SparksPropertyTests
{
    private const int SparksCliloc = 1157326;
    private static readonly Point3D TestLocation = new(6200, 500, 0);

    [Fact]
    public void ExtendedWeaponAttributes_StoresAndDupesSparks()
    {
        var weapon = new TestKatana();
        var dupe = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.HitSparks = 20;

            weapon.Dupe(dupe);

            Assert.Equal(20, weapon.ExtendedWeaponAttributes.HitSparks);
            Assert.Equal(20, dupe.ExtendedWeaponAttributes.HitSparks);
        }
        finally
        {
            weapon.Delete();
            dupe.Delete();
        }
    }

    [Fact]
    public void ExtendedWeaponAttributes_GetProperties_GatesSparksTooltipToTimeOfLegends()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.HitSparks = 20;

            Core.Expansion = Expansion.HS;
            var preTimeOfLegends = new RecordingPropertyList();
            weapon.ExtendedWeaponAttributes.GetProperties(preTimeOfLegends);
            Assert.DoesNotContain(preTimeOfLegends.Entries, entry => entry.Number == SparksCliloc);

            Core.Expansion = Expansion.TOL;
            var timeOfLegends = new RecordingPropertyList();
            weapon.ExtendedWeaponAttributes.GetProperties(timeOfLegends);
            Assert.Contains(
                timeOfLegends.Entries,
                entry => entry.Number == SparksCliloc && entry.Argument == "20"
            );
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    [Fact]
    public void OnHit_PreTimeOfLegends_DoesNotStartSparksContext()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.ExtendedWeaponAttributes.HitSparks = 100;

            weapon.OnHit(attacker, defender);

            Assert.False(Sparks.HasActiveContext(attacker, defender));
            Assert.Equal(0, Sparks.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Sparks.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_NormalSuccessfulHit_StartsOneSparksContextAndRejectsDuplicateProc()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            weapon.ExtendedWeaponAttributes.HitSparks = 100;

            weapon.OnHit(attacker, defender);

            Assert.True(Sparks.HasActiveContext(attacker, defender));
            Assert.Equal(1, Sparks.ActiveContextCount);
            Assert.Equal(Sparks.TickCount, Sparks.GetTicksRemainingForTests(attacker, defender));

            weapon.OnHit(attacker, defender);

            Assert.True(Sparks.HasActiveContext(attacker, defender));
            Assert.Equal(1, Sparks.ActiveContextCount);
            Assert.Equal(Sparks.TickCount, Sparks.GetTicksRemainingForTests(attacker, defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Sparks.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_CurrentWeaponAbility_DoesNotStartSparksContext()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            weapon.ExtendedWeaponAttributes.HitSparks = 100;
            WeaponAbility.Table[attacker] = new TestWeaponAbility();

            weapon.OnHit(attacker, defender);

            Assert.False(Sparks.HasActiveContext(attacker, defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            WeaponAbility.Table.Remove(attacker);
            Sparks.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_CurrentGenericSpecialMove_DoesNotStartSparksContext()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            weapon.ExtendedWeaponAttributes.HitSparks = 100;
            SpecialMove.Table[attacker] = new TestSpecialMove();

            weapon.OnHit(attacker, defender);

            Assert.False(Sparks.HasActiveContext(attacker, defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            SpecialMove.Table.Remove(attacker);
            Sparks.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Theory]
    [InlineData(typeof(LightningStrike))]
    [InlineData(typeof(DeathStrike))]
    public void OnHit_NamedSpecialMoveExclusions_DoNotStartSparksContext(Type moveType)
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            weapon.ExtendedWeaponAttributes.HitSparks = 100;
            SpecialMove.Table[attacker] = (SpecialMove)Activator.CreateInstance(moveType);

            weapon.OnHit(attacker, defender);

            Assert.False(Sparks.HasActiveContext(attacker, defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            SpecialMove.Table.Remove(attacker);
            Sparks.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void ApplySparksTick_PlayerTarget_UsesEnergyResistanceAndReturnsAppliedDamageAsMana()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile(mana: 10);
        var defender = CreateMobile(hitsMax: 200, hits: 200, player: true, location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            AddResistance(defender, ResistanceType.Energy, 70);

            var damageApplied = Sparks.ApplySparksTick(attacker, defender, 20);

            Assert.Equal(6, damageApplied);
            Assert.Equal(194, defender.Hits);
            Assert.Equal(16, attacker.Mana);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Sparks.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void ApplySparksTick_NonPlayerTarget_DoublesRawDamageBeforeResistance()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile(mana: 10);
        var defender = CreateMobile(hitsMax: 200, hits: 200, location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            AddResistance(defender, ResistanceType.Energy, 70);

            var damageApplied = Sparks.ApplySparksTick(attacker, defender, 20);

            Assert.Equal(12, damageApplied);
            Assert.Equal(188, defender.Hits);
            Assert.Equal(22, attacker.Mana);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Sparks.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void ApplySparksTick_ManaReturnIsClampedByManaMax()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile(mana: 95);
        var defender = CreateMobile(hitsMax: 200, hits: 200, player: true, location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.TOL;

            var damageApplied = Sparks.ApplySparksTick(attacker, defender, 20);

            Assert.Equal(20, damageApplied);
            Assert.Equal(180, defender.Hits);
            Assert.Equal(attacker.ManaMax, attacker.Mana);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Sparks.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void SparksContext_TicksFiveTimesAtOneSecondIntervalAndThenCleansUp()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile(mana: 0);
        var defender = CreateMobile(hitsMax: 500, hits: 500, player: true, location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            Sparks.RawDamageOverrideForTests = 20;

            Sparks.TryProcOnNormalHit(attacker, defender, 100);

            Assert.True(Sparks.HasActiveContext(attacker, defender));
            Assert.Equal(TimeSpan.FromSeconds(1.0), Sparks.TickInterval);

            for (var i = 0; i < Sparks.TickCount; i++)
            {
                Assert.True(Sparks.TickForTests(attacker, defender));
            }

            Assert.False(Sparks.HasActiveContext(attacker, defender));
            Assert.Equal(400, defender.Hits);
            Assert.Equal(100, attacker.Mana);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Sparks.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void SparksContext_TickCleansUpInternalizedCombatant()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.TOL;

            Sparks.TryProcOnNormalHit(attacker, defender, 100);
            defender.Internalize();

            Assert.True(Sparks.TickForTests(attacker, defender));
            Assert.False(Sparks.HasActiveContext(attacker, defender));
            Assert.Equal(0, Sparks.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Sparks.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void SparksClear_RemovesContextsForDeletedOrDeadEventMobile()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.TOL;

            Sparks.TryProcOnNormalHit(attacker, defender, 100);
            Sparks.Clear(defender);

            Assert.False(Sparks.HasActiveContext(attacker, defender));
            Assert.Equal(0, Sparks.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Sparks.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void RunicAttributeGeneration_DoesNotRollSparks()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            Core.Expansion = Expansion.TOL;

            BaseRunicTool.ApplyAttributesTo(weapon, false, 0, 25, 100, 100);

            Assert.Equal(0, weapon.ExtendedWeaponAttributes.HitSparks);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    private static Mobile CreateMobile(
        int hitsMax = 200,
        int hits = 200,
        int manaMax = 100,
        int mana = 0,
        bool player = false,
        Point3D location = default
    )
    {
        var mobile = new Mobile(World.NewMobile)
        {
            Player = player
        };

        mobile.DefaultMobileInit();
        mobile.RawStr = Math.Max(1, (hitsMax - 50) * 2);
        mobile.RawInt = manaMax;
        Assert.Equal(hitsMax, mobile.HitsMax);
        Assert.Equal(manaMax, mobile.ManaMax);
        mobile.Hits = hits;
        mobile.Mana = mana;
        mobile.MoveToWorld(location == default ? TestLocation : location, Map.Felucca);
        return mobile;
    }

    private static void AddResistance(Mobile mobile, ResistanceType type, int offset) =>
        mobile.AddResistanceMod(new ResistanceMod(type, $"SparksTest{type}{mobile.Serial}", offset, mobile));

    private class TestKatana : Katana
    {
        public override bool CheckHit(Mobile attacker, Mobile defender) => true;
        public override int ComputeDamage(Mobile attacker, Mobile defender) => 1;
        public override int AbsorbDamage(Mobile attacker, Mobile defender, int damage) => damage;
        public override void AddBlood(Mobile attacker, Mobile defender, int damage)
        {
        }
    }

    private class TestWeaponAbility : WeaponAbility
    {
    }

    private class TestSpecialMove : SpecialMove
    {
    }

    private sealed class RecordingPropertyList : IPropertyList
    {
        private string _interpolated = string.Empty;

        public List<(int Number, string Argument)> Entries { get; } = [];

        public void Reset()
        {
        }

        public void Terminate()
        {
        }

        public void Add(int number) => Entries.Add((number, null));
        public void Add(int number, string argument) => Entries.Add((number, argument));
        public void Add(ReadOnlySpan<char> argument) => Entries.Add((0, argument.ToString()));
        public void Add(int number, ReadOnlySpan<char> argument) => Entries.Add((number, argument.ToString()));
        public void AddChunked(ReadOnlySpan<char> text) => Entries.Add((0, text.ToString()));
        public OplTextBlock TextBlock() => new(this);
        public void Add(int number, int value) => Entries.Add((number, value.ToString(CultureInfo.InvariantCulture)));
        public void AddLocalized(int value) => Entries.Add((0, value.ToString(CultureInfo.InvariantCulture)));
        public void AddLocalized(int number, int value) => Entries.Add((number, value.ToString(CultureInfo.InvariantCulture)));
        public void Add(ref IPropertyList.InterpolatedStringHandler handler) => Entries.Add((0, _interpolated));
        public void Add(int number, ref IPropertyList.InterpolatedStringHandler handler) => Entries.Add((number, _interpolated));
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
