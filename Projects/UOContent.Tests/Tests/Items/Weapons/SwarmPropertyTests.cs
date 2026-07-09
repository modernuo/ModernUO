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
public class SwarmPropertyTests
{
    private const int SwarmCliloc = 1157325;
    private static readonly Point3D TestLocation = new(6200, 520, 0);

    [Fact]
    public void ExtendedWeaponAttributes_StoresAndDupesSwarm()
    {
        var weapon = new TestKatana();
        var dupe = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.HitSwarm = 30;

            weapon.Dupe(dupe);

            Assert.Equal(30, weapon.ExtendedWeaponAttributes.HitSwarm);
            Assert.Equal(30, dupe.ExtendedWeaponAttributes.HitSwarm);
        }
        finally
        {
            weapon.Delete();
            dupe.Delete();
        }
    }

    [Fact]
    public void ExtendedWeaponAttributes_GetProperties_GatesSwarmTooltipToTimeOfLegends()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.HitSwarm = 30;

            Core.Expansion = Expansion.HS;
            var preTimeOfLegends = new RecordingPropertyList();
            weapon.ExtendedWeaponAttributes.GetProperties(preTimeOfLegends);
            Assert.DoesNotContain(preTimeOfLegends.Entries, entry => entry.Number == SwarmCliloc);

            Core.Expansion = Expansion.TOL;
            var timeOfLegends = new RecordingPropertyList();
            weapon.ExtendedWeaponAttributes.GetProperties(timeOfLegends);
            Assert.Contains(
                timeOfLegends.Entries,
                entry => entry.Number == SwarmCliloc && entry.Argument == "30"
            );
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    [Fact]
    public void OnHit_PreTimeOfLegends_DoesNotStartSwarmContext()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.ExtendedWeaponAttributes.HitSwarm = 100;

            weapon.OnHit(attacker, defender);

            Assert.False(Swarm.HasActiveContext(attacker, defender));
            Assert.Equal(0, Swarm.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Swarm.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_NormalSuccessfulHit_StartsOneSwarmContextAndRejectsDuplicateDefenderProc()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile();
        var secondAttacker = CreateMobile(location: new Point3D(6199, 520, 0));
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            weapon.ExtendedWeaponAttributes.HitSwarm = 100;

            weapon.OnHit(attacker, defender);

            Assert.True(Swarm.HasActiveContext(attacker, defender));
            Assert.True(Swarm.HasActiveContext(defender));
            Assert.Equal(1, Swarm.ActiveContextCount);
            Assert.Equal(Swarm.TickCount, Swarm.GetTicksRemainingForTests(attacker, defender));

            Swarm.TryProcOnNormalHit(secondAttacker, defender, 100);

            Assert.True(Swarm.HasActiveContext(attacker, defender));
            Assert.False(Swarm.HasActiveContext(secondAttacker, defender));
            Assert.Equal(1, Swarm.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Swarm.ClearAll();
            weapon.Delete();
            attacker.Delete();
            secondAttacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_CurrentWeaponAbility_DoesNotStartSwarmContext()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            weapon.ExtendedWeaponAttributes.HitSwarm = 100;
            WeaponAbility.Table[attacker] = new TestWeaponAbility();

            weapon.OnHit(attacker, defender);

            Assert.False(Swarm.HasActiveContext(attacker, defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            WeaponAbility.Table.Remove(attacker);
            Swarm.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Theory]
    [InlineData(typeof(TestSpecialMove))]
    [InlineData(typeof(LightningStrike))]
    [InlineData(typeof(DeathStrike))]
    public void OnHit_SpecialMoves_DoNotStartSwarmContext(Type moveType)
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            weapon.ExtendedWeaponAttributes.HitSwarm = 100;
            SpecialMove.Table[attacker] = (SpecialMove)Activator.CreateInstance(moveType);

            weapon.OnHit(attacker, defender);

            Assert.False(Swarm.HasActiveContext(attacker, defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            SpecialMove.Table.Remove(attacker);
            Swarm.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void ApplySwarmTick_UsesPhysicalResistanceAndReturnsAppliedDamage()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(hitsMax: 200, hits: 200, location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            AddResistance(defender, ResistanceType.Physical, 70);

            var damageApplied = Swarm.ApplySwarmTick(attacker, defender, Swarm.RawDamage);

            Assert.Equal(3, damageApplied);
            Assert.Equal(197, defender.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Swarm.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void SwarmContext_TicksThreeTimesAtFiveSecondIntervalAndThenCleansUp()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(hitsMax: 200, hits: 200, location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;

            Swarm.TryProcOnNormalHit(attacker, defender, 100);

            Assert.True(Swarm.HasActiveContext(attacker, defender));
            Assert.Equal(TimeSpan.FromSeconds(5.0), Swarm.TickInterval);
            Assert.Equal(TimeSpan.FromSeconds(15.0), Swarm.EffectDuration);

            for (var i = 0; i < Swarm.TickCount; i++)
            {
                Assert.True(Swarm.TickForTests(attacker, defender));
            }

            Assert.False(Swarm.HasActiveContext(attacker, defender));
            Assert.Equal(170, defender.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Swarm.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void PositivePostResistFireDamageClearsSwarmContext()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            Swarm.TryProcOnNormalHit(attacker, defender, 100);

            AOS.Damage(defender, attacker, 10, false, 0, 100, 0, 0, 0);

            Assert.False(Swarm.HasActiveContext(attacker, defender));
            Assert.Equal(0, Swarm.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Swarm.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void FullyResistedFireDamageDoesNotClearSwarmContext()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            AddResistance(defender, ResistanceType.Fire, 100);
            Swarm.TryProcOnNormalHit(attacker, defender, 100);

            AOS.Damage(defender, attacker, 10, false, 0, 100, 0, 0, 0);

            Assert.True(Swarm.HasActiveContext(attacker, defender));
            Assert.Equal(1, Swarm.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Swarm.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void BurningTorchEquipClearsSwarmContextImmediately()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));
        var torch = new Torch();

        try
        {
            Core.Expansion = Expansion.TOL;
            Swarm.TryProcOnNormalHit(attacker, defender, 100);
            torch.Burning = true;

            Assert.True(defender.EquipItem(torch));

            Assert.False(Swarm.HasActiveContext(attacker, defender));
            Assert.Equal(0, Swarm.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Swarm.ClearAll();
            torch.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void BurningTorchIgniteClearsSwarmContextImmediately()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));
        var torch = new Torch();

        try
        {
            Core.Expansion = Expansion.TOL;
            Swarm.TryProcOnNormalHit(attacker, defender, 100);
            Assert.True(defender.EquipItem(torch));

            torch.Ignite();

            Assert.False(Swarm.HasActiveContext(attacker, defender));
            Assert.Equal(0, Swarm.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Swarm.ClearAll();
            torch.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void SwarmTickWithBurningTorchClearsContextWithoutDamage()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));
        var torch = new Torch();

        try
        {
            Core.Expansion = Expansion.TOL;
            Assert.True(defender.EquipItem(torch));
            Swarm.TryProcOnNormalHit(attacker, defender, 100);
            torch.Burning = true;

            var damageApplied = Swarm.ApplySwarmTick(attacker, defender, Swarm.RawDamage);

            Assert.Equal(0, damageApplied);
            Assert.Equal(200, defender.Hits);
            Assert.False(Swarm.HasActiveContext(attacker, defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Swarm.ClearAll();
            torch.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void SwarmContext_TickCleansUpInternalizedCombatant()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;

            Swarm.TryProcOnNormalHit(attacker, defender, 100);
            defender.Internalize();

            Assert.True(Swarm.TickForTests(attacker, defender));
            Assert.False(Swarm.HasActiveContext(attacker, defender));
            Assert.Equal(0, Swarm.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Swarm.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void SwarmClear_RemovesContextsForDeletedOrDeadEventMobile()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;

            Swarm.TryProcOnNormalHit(attacker, defender, 100);
            Swarm.Clear(defender);

            Assert.False(Swarm.HasActiveContext(attacker, defender));
            Assert.Equal(0, Swarm.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Swarm.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void RunicAttributeGeneration_DoesNotRollSwarm()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            Core.Expansion = Expansion.TOL;

            BaseRunicTool.ApplyAttributesTo(weapon, false, 0, 25, 100, 100);

            Assert.Equal(0, weapon.ExtendedWeaponAttributes.HitSwarm);
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
        Point3D location = default
    )
    {
        var mobile = new Mobile(World.NewMobile);

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
        mobile.AddResistanceMod(new ResistanceMod(type, $"SwarmTest{type}{mobile.Serial}", offset, mobile));

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
