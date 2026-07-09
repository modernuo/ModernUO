using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class SplinteringWeaponPropertyTests
{
    private const int SplinteringWeaponCliloc = 1112857;
    private static readonly Point3D TestLocation = new(6200, 540, 0);

    [Fact]
    public void ExtendedWeaponAttributes_StoresAndDupesSplinteringWeapon()
    {
        var weapon = new TestKatana();
        var dupe = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.SplinteringWeapon = 30;

            weapon.Dupe(dupe);

            Assert.Equal(30, weapon.ExtendedWeaponAttributes.SplinteringWeapon);
            Assert.Equal(30, dupe.ExtendedWeaponAttributes.SplinteringWeapon);
        }
        finally
        {
            weapon.Delete();
            dupe.Delete();
        }
    }

    [Fact]
    public void ExtendedWeaponAttributes_GetProperties_GatesSplinteringWeaponTooltipToStygianAbyss()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.SplinteringWeapon = 30;

            Core.Expansion = Expansion.ML;
            var preStygianAbyss = new RecordingPropertyList();
            weapon.ExtendedWeaponAttributes.GetProperties(preStygianAbyss);
            Assert.DoesNotContain(preStygianAbyss.Entries, entry => entry.Number == SplinteringWeaponCliloc);

            Core.Expansion = Expansion.SA;
            var stygianAbyss = new RecordingPropertyList();
            weapon.ExtendedWeaponAttributes.GetProperties(stygianAbyss);
            Assert.Contains(
                stygianAbyss.Entries,
                entry => entry.Number == SplinteringWeaponCliloc && entry.Argument == "30"
            );
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    [Fact]
    public void OnHit_PreStygianAbyss_DoesNotStartSplinteringContext()
    {
        var previousExpansion = Core.Expansion;
        var weapon = EquipSplinteringWeapon(CreateMobile());
        var attacker = (Mobile)weapon.Parent;
        var defender = CreateMobile(location: new Point3D(6201, 540, 0));

        try
        {
            Core.Expansion = Expansion.ML;

            weapon.OnHit(attacker, defender);

            Assert.Equal(0, SplinteringWeapon.ActiveContextCount);
            Assert.False(SplinteringWeapon.IsForceWalking(defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            SplinteringWeapon.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_EligibleSuccessfulHit_StartsBleedForceWalkAndDamagesDurability()
    {
        var previousExpansion = Core.Expansion;
        var weapon = EquipSplinteringWeapon(CreateMobile());
        var attacker = (Mobile)weapon.Parent;
        var defender = CreateMobile(location: new Point3D(6201, 540, 0));

        try
        {
            Core.Expansion = Expansion.SA;
            weapon.HitPoints = 50;
            weapon.MaxHitPoints = 50;
            var defenderHits = defender.Hits;

            weapon.OnHit(attacker, defender);

            Assert.True(weapon.HitPoints <= 40);
            Assert.True(SplinteringWeapon.IsForceWalking(defender));
            Assert.Equal(1, SplinteringWeapon.ActiveContextCount);

            Assert.True(SplinteringWeapon.TickForTests(attacker, defender));

            Assert.True(defender.Hits < defenderHits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            SplinteringWeapon.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void ForceWalk_EndsAfterFourSecondsWhileSplinteringBleedCanContinue()
    {
        var previousExpansion = Core.Expansion;
        var weapon = EquipSplinteringWeapon(CreateMobile());
        var attacker = (Mobile)weapon.Parent;
        var defender = CreateMobile(location: new Point3D(6201, 540, 0));

        try
        {
            Core.Expansion = Expansion.SA;

            weapon.OnHit(attacker, defender);
            Assert.True(SplinteringWeapon.IsForceWalking(defender));

            Assert.True(SplinteringWeapon.EndForceWalkForTests(attacker, defender));

            Assert.False(SplinteringWeapon.IsForceWalking(defender));
            Assert.Equal(1, SplinteringWeapon.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            SplinteringWeapon.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void ForceWalk_BlocksRunningMovementUntilEffectEnds()
    {
        var previousExpansion = Core.Expansion;
        var weapon = EquipSplinteringWeapon(CreateMobile());
        var attacker = (Mobile)weapon.Parent;
        var defender = CreateMobile(location: new Point3D(6201, 540, 0));

        try
        {
            Core.Expansion = Expansion.SA;

            weapon.OnHit(attacker, defender);
            var run = MovementEventArgs.Create(defender, Direction.Running);
            SplinteringWeapon.OnMovement(run);
            Assert.True(run.Blocked);
            run.Free();

            var walk = MovementEventArgs.Create(defender, Direction.North);
            SplinteringWeapon.OnMovement(walk);
            Assert.False(walk.Blocked);
            walk.Free();

            Assert.True(SplinteringWeapon.EndForceWalkForTests(attacker, defender));
            var afterEffect = MovementEventArgs.Create(defender, Direction.Running);
            SplinteringWeapon.OnMovement(afterEffect);
            Assert.False(afterEffect.Blocked);
            afterEffect.Free();
        }
        finally
        {
            Core.Expansion = previousExpansion;
            SplinteringWeapon.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void LogoutClearsSplinteringEffectsAndPlayerImmunity()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile();
        var defender = CreatePlayerMobile(location: new Point3D(6201, 540, 0));

        try
        {
            Core.Expansion = Expansion.SA;
            Assert.True(SplinteringWeapon.TryProcOnEligibleHit(attacker, defender, weapon, null, 100));
            Assert.True(SplinteringWeapon.IsForceWalking(defender));
            Assert.True(SplinteringWeapon.IsPlayerImmune(defender));

            SplinteringWeapon.Clear(defender);

            Assert.False(SplinteringWeapon.IsForceWalking(defender));
            Assert.False(SplinteringWeapon.IsPlayerImmune(defender));
            Assert.Equal(0, SplinteringWeapon.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            SplinteringWeapon.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void PlayerVictim_ReceivesFifteenSecondImmunityToRepeatedSplinteringEffects()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile();
        var defender = CreatePlayerMobile(location: new Point3D(6201, 540, 0));

        try
        {
            Core.Expansion = Expansion.SA;

            Assert.True(SplinteringWeapon.TryProcOnEligibleHit(attacker, defender, weapon, null, 100));
            Assert.True(SplinteringWeapon.IsPlayerImmune(defender));

            Assert.True(SplinteringWeapon.EndForceWalkForTests(attacker, defender));
            Assert.False(SplinteringWeapon.IsForceWalking(defender));

            Assert.False(SplinteringWeapon.TryProcOnEligibleHit(attacker, defender, weapon, null, 100));

            Assert.False(SplinteringWeapon.IsForceWalking(defender));

            SplinteringWeapon.ExpirePlayerImmunityForTests(defender);
            Assert.False(SplinteringWeapon.IsPlayerImmune(defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            SplinteringWeapon.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Theory]
    [InlineData(5)]
    [InlineData(8)]
    public void NamedExcludedWeaponAbilities_DoNotTriggerSplinteringWeapon(int abilityIndex)
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 540, 0));

        try
        {
            Core.Expansion = Expansion.SA;
            var triggered = SplinteringWeapon.TryProcOnEligibleHit(
                attacker,
                defender,
                weapon,
                WeaponAbility.Abilities[abilityIndex],
                100
            );

            Assert.False(triggered);
            Assert.False(SplinteringWeapon.IsForceWalking(defender));
            Assert.Equal(0, SplinteringWeapon.ActiveContextCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            SplinteringWeapon.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void NonExcludedWeaponAbility_CanTriggerSplinteringWeapon()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 540, 0));

        try
        {
            Core.Expansion = Expansion.SA;
            var triggered = SplinteringWeapon.TryProcOnEligibleHit(
                attacker,
                defender,
                weapon,
                WeaponAbility.ArmorIgnore,
                100
            );

            Assert.True(triggered);
            Assert.True(SplinteringWeapon.IsForceWalking(defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            SplinteringWeapon.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void SplinteringBleed_DoesNotReplaceRegularBleedAttackContext()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 540, 0));

        try
        {
            Core.Expansion = Expansion.SA;
            BleedAttack.BeginBleed(defender, attacker);
            Assert.True(BleedAttack.IsBleeding(defender));

            var triggered = SplinteringWeapon.TryProcOnEligibleHit(attacker, defender, weapon, null, 100);

            Assert.True(triggered);
            Assert.True(BleedAttack.IsBleeding(defender));
            Assert.True(SplinteringWeapon.IsForceWalking(defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            BleedAttack.EndBleed(defender, false);
            SplinteringWeapon.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void RunicAttributeGeneration_DoesNotRollSplinteringWeapon()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            Core.Expansion = Expansion.SA;

            BaseRunicTool.ApplyAttributesTo(weapon, false, 0, 25, 100, 100);

            Assert.Equal(0, weapon.ExtendedWeaponAttributes.SplinteringWeapon);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    private static TestKatana EquipSplinteringWeapon(Mobile wielder)
    {
        var weapon = new TestKatana();
        weapon.ExtendedWeaponAttributes.SplinteringWeapon = 100;
        wielder.AddItem(weapon);
        return weapon;
    }

    private static Mobile CreateMobile(bool player = false, Point3D location = default)
    {
        var mobile = new Mobile(World.NewMobile)
        {
            Player = player
        };

        mobile.DefaultMobileInit();
        InitializeHits(mobile, 500, 500);
        mobile.MoveToWorld(location == default ? TestLocation : location, Map.Felucca);
        return mobile;
    }

    private static PlayerMobile CreatePlayerMobile(Point3D location)
    {
        var mobile = new PlayerMobile(World.NewMobile);

        mobile.DefaultMobileInit();
        mobile.Player = true;
        mobile.Hits = mobile.HitsMax;
        mobile.MoveToWorld(location, Map.Felucca);
        return mobile;
    }

    private static void InitializeHits(Mobile mobile, int hitsMax, int hits)
    {
        mobile.RawStr = Math.Max(1, (hitsMax - 50) * 2);
        Assert.Equal(hitsMax, mobile.HitsMax);
        mobile.Hits = hits;
    }

    private class TestKatana : Katana
    {
        public override int ComputeDamage(Mobile attacker, Mobile defender) => 100;
        public override int AbsorbDamage(Mobile attacker, Mobile defender, int damage) => damage;
        public override void AddBlood(Mobile attacker, Mobile defender, int damage)
        {
        }
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

        public void Add(int number) => Entries.Add((number, string.Empty));
        public void Add(int number, string argument) => Entries.Add((number, argument));
        public void Add(ReadOnlySpan<char> argument) => Entries.Add((0, argument.ToString()));
        public void Add(int number, ReadOnlySpan<char> argument) => Entries.Add((number, argument.ToString()));
        public void AddChunked(ReadOnlySpan<char> text) => Entries.Add((0, text.ToString()));
        public OplTextBlock TextBlock() => new(this);
        public void Add(int number, int value) => Entries.Add((number, value.ToString()));
        public void AddLocalized(int value) => Entries.Add((0, value.ToString()));
        public void AddLocalized(int number, int value) => Entries.Add((number, value.ToString()));
        public void Add(ref IPropertyList.InterpolatedStringHandler handler) => Entries.Add((0, _interpolated));
        public void Add(int number, ref IPropertyList.InterpolatedStringHandler handler) =>
            Entries.Add((number, _interpolated));
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
