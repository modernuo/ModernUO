using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BattleLustPropertyTests
{
    private const int BattleLustCliloc = 1113710;
    private static readonly DateTime TestNow = new(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ExtendedWeaponAttributes_StoresAndDupesBattleLust()
    {
        var weapon = new TestKatana();
        var dupe = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.BattleLust = 1;

            weapon.Dupe(dupe);

            Assert.Equal(1, weapon.ExtendedWeaponAttributes.BattleLust);
            Assert.Equal(1, dupe.ExtendedWeaponAttributes.BattleLust);
        }
        finally
        {
            weapon.Delete();
            dupe.Delete();
        }
    }

    [Fact]
    public void ExtendedWeaponAttributes_GetProperties_GatesBattleLustTooltipToStygianAbyss()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.BattleLust = 1;

            Core.Expansion = Expansion.ML;
            var preStygianAbyss = new RecordingPropertyList();
            weapon.ExtendedWeaponAttributes.GetProperties(preStygianAbyss);
            Assert.DoesNotContain(preStygianAbyss.Numbers, number => number == BattleLustCliloc);

            Core.Expansion = Expansion.SA;
            var stygianAbyss = new RecordingPropertyList();
            weapon.ExtendedWeaponAttributes.GetProperties(stygianAbyss);
            Assert.Contains(stygianAbyss.Numbers, number => number == BattleLustCliloc);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    [Fact]
    public void DamageTaken_PreStygianAbyss_DoesNotGainBattleLustPoints()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var wielder = CreateMobile();
        var source = CreateMobile(location: new Point3D(6201, 500, 0));
        var weapon = EquipBattleLustWeapon(wielder);

        try
        {
            Core.Expansion = Expansion.ML;
            Core._now = TestNow;

            ApplyDamage(wielder, source, BattleLust.DamageThreshold);

            Assert.Equal(0, BattleLust.GetPoints(wielder));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Core._now = previousNow;
            BattleLust.Clear(wielder);
            weapon.Delete();
            wielder.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void DamageTaken_GainsOnlyFromQualifyingExternalLivingMobileDamage()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var wielder = CreateMobile(hitsMax: 5000, hits: 5000);
        var source = CreateMobile(location: new Point3D(6201, 500, 0));
        var deletedSource = CreateMobile(location: new Point3D(6202, 500, 0));
        var deadSource = CreateDeadPlayerMobile(new Point3D(6203, 500, 0));
        var weapon = EquipBattleLustWeapon(wielder);

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;

            ApplyDamage(wielder, source, BattleLust.DamageThreshold - 1);
            Assert.Equal(0, BattleLust.GetPoints(wielder));

            Core._now = Core._now.AddSeconds(2);
            ApplyDamage(wielder, null, BattleLust.DamageThreshold);
            Assert.Equal(0, BattleLust.GetPoints(wielder));

            Core._now = Core._now.AddSeconds(2);
            ApplyDamage(wielder, wielder, BattleLust.DamageThreshold);
            Assert.Equal(0, BattleLust.GetPoints(wielder));

            deletedSource.Delete();
            Core._now = Core._now.AddSeconds(2);
            ApplyDamage(wielder, deletedSource, BattleLust.DamageThreshold);
            Assert.Equal(0, BattleLust.GetPoints(wielder));

            Core._now = Core._now.AddSeconds(2);
            ApplyDamage(wielder, deadSource, BattleLust.DamageThreshold);
            Assert.Equal(0, BattleLust.GetPoints(wielder));

            Core._now = Core._now.AddSeconds(2);
            ApplyDamage(wielder, source, BattleLust.DamageThreshold);
            Assert.Equal(1, BattleLust.GetPoints(wielder));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Core._now = previousNow;
            BattleLust.Clear(wielder);
            weapon.Delete();
            wielder.Delete();
            source.Delete();
            deletedSource.Delete();
            deadSource.Delete();
        }
    }

    [Fact]
    public void DamageTaken_DoesNotGainWhenMobileDamageAppliesNoHitPointLoss()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var wielder = CreateMobile(hitsMax: 5000, hits: 5000);
        var source = CreateMobile(location: new Point3D(6201, 500, 0));
        var weapon = EquipBattleLustWeapon(wielder);

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;
            wielder.Blessed = true;

            ApplyDamage(wielder, source, BattleLust.DamageThreshold);

            Assert.Equal(5000, wielder.Hits);
            Assert.Equal(0, BattleLust.GetPoints(wielder));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Core._now = previousNow;
            BattleLust.Clear(wielder);
            weapon.Delete();
            wielder.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void DamageTaken_GainCooldownAllowsOnlyOnePointEveryTwoSecondsAndCapsAtFifteen()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var wielder = CreateMobile(hitsMax: 5000, hits: 5000);
        var source = CreateMobile(location: new Point3D(6201, 500, 0));
        var weapon = EquipBattleLustWeapon(wielder);

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;

            ApplyDamage(wielder, source, BattleLust.DamageThreshold);
            Assert.Equal(1, BattleLust.GetPoints(wielder));

            Core._now = Core._now.AddSeconds(1);
            ApplyDamage(wielder, source, BattleLust.DamageThreshold);
            Assert.Equal(1, BattleLust.GetPoints(wielder));

            Core._now = Core._now.AddSeconds(1);
            ApplyDamage(wielder, source, BattleLust.DamageThreshold);
            Assert.Equal(2, BattleLust.GetPoints(wielder));

            for (var i = 0; i < 20; i++)
            {
                Core._now = Core._now.AddSeconds(2);
                ApplyDamage(wielder, source, BattleLust.DamageThreshold);
            }

            Assert.Equal(BattleLust.MaxPoints, BattleLust.GetPoints(wielder));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Core._now = previousNow;
            BattleLust.Clear(wielder);
            weapon.Delete();
            wielder.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void DamageTaken_DecaysOnePointEverySixSecondsAndCleansUpAtZero()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var wielder = CreateMobile();
        var source = CreateMobile(location: new Point3D(6201, 500, 0));
        var weapon = EquipBattleLustWeapon(wielder);

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;

            ApplyDamage(wielder, source, BattleLust.DamageThreshold);
            Assert.Equal(1, BattleLust.GetPoints(wielder));

            Core._now = Core._now.AddSeconds(6);
            Core._tickCount += 6000;
            Timer.Slice(Core._tickCount);

            Assert.Equal(0, BattleLust.GetPoints(wielder));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Core._now = previousNow;
            BattleLust.Clear(wielder);
            weapon.Delete();
            wielder.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void RuntimeContext_CleansUpWhenOwnerDiesOrLosesBattleLustWeapon()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var deadWielder = CreateMobile();
        var disarmedWielder = CreateMobile(location: new Point3D(6202, 500, 0));
        var source = CreateMobile(location: new Point3D(6201, 500, 0));
        var deadWeapon = EquipBattleLustWeapon(deadWielder);
        var disarmedWeapon = EquipBattleLustWeapon(disarmedWielder);

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;

            ApplyDamage(deadWielder, source, BattleLust.DamageThreshold);
            Assert.Equal(1, BattleLust.GetPoints(deadWielder));
            deadWielder.Kill();
            Assert.Equal(0, BattleLust.GetPoints(deadWielder));

            ApplyDamage(disarmedWielder, source, BattleLust.DamageThreshold);
            Assert.Equal(1, BattleLust.GetPoints(disarmedWielder));
            disarmedWeapon.ExtendedWeaponAttributes.BattleLust = 0;
            Assert.Equal(0, BattleLust.GetDamageBonus(disarmedWielder, source));
            Assert.Equal(0, BattleLust.GetPoints(disarmedWielder));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Core._now = previousNow;
            BattleLust.Clear(deadWielder);
            BattleLust.Clear(disarmedWielder);
            deadWeapon.Delete();
            disarmedWeapon.Delete();
            deadWielder.Delete();
            disarmedWielder.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void RuntimeContext_ClearsOnWeaponLossEvenIfReequippedBeforeLazyCleanup()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var wielder = CreateMobile();
        var source = CreateMobile(location: new Point3D(6201, 500, 0));
        var weapon = EquipBattleLustWeapon(wielder);

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;

            ApplyDamage(wielder, source, BattleLust.DamageThreshold);
            Assert.Equal(1, BattleLust.GetPoints(wielder));

            wielder.RemoveItem(weapon);
            wielder.AddItem(weapon);

            Assert.Equal(0, BattleLust.GetPoints(wielder));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Core._now = previousNow;
            BattleLust.Clear(wielder);
            weapon.Delete();
            wielder.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void RuntimeContext_ClearsWhenBattleLustPropertyIsRemovedEvenIfRestoredBeforeLazyCleanup()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var wielder = CreateMobile();
        var source = CreateMobile(location: new Point3D(6201, 500, 0));
        var weapon = EquipBattleLustWeapon(wielder);

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;

            ApplyDamage(wielder, source, BattleLust.DamageThreshold);
            Assert.Equal(1, BattleLust.GetPoints(wielder));

            weapon.ExtendedWeaponAttributes.BattleLust = 0;
            weapon.ExtendedWeaponAttributes.BattleLust = 1;

            Assert.Equal(0, BattleLust.GetPoints(wielder));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Core._now = previousNow;
            BattleLust.Clear(wielder);
            weapon.Delete();
            wielder.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void RuntimeContext_ClearsOnInvalidMapEvenIfOwnerReturnsBeforeDecayTick()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var wielder = CreateMobile();
        var source = CreateMobile(location: new Point3D(6201, 500, 0));
        var weapon = EquipBattleLustWeapon(wielder);

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;

            ApplyDamage(wielder, source, BattleLust.DamageThreshold);
            Assert.Equal(1, BattleLust.GetPoints(wielder));

            wielder.MoveToWorld(Point3D.Zero, Map.Internal);
            wielder.MoveToWorld(new Point3D(6200, 500, 0), Map.Felucca);

            Assert.Equal(0, BattleLust.GetPoints(wielder));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Core._now = previousNow;
            BattleLust.Clear(wielder);
            weapon.Delete();
            wielder.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void GetDamageBonus_UsesAggressedCountAndPropertySpecificPvpPvmCaps()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var attacker = CreateMobile(hitsMax: 5000, hits: 5000);
        var source = CreateMobile(location: new Point3D(6201, 500, 0));
        var playerDefender = CreateMobile(player: true, location: new Point3D(6202, 500, 0));
        var creatureDefender = CreateMobile(location: new Point3D(6203, 500, 0));
        var weapon = EquipBattleLustWeapon(attacker);

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;
            BuildBattleLustPoints(attacker, source, BattleLust.MaxPoints);

            Assert.Equal(0, BattleLust.GetDamageBonus(attacker, creatureDefender));

            AddAggressed(attacker, creatureDefender);
            Assert.Equal(15, BattleLust.GetDamageBonus(attacker, creatureDefender));

            AddAggressed(attacker, CreateMobile(location: new Point3D(6204, 500, 0)));
            AddAggressed(attacker, playerDefender);
            Assert.Equal(BattleLust.PvpDamageCap, BattleLust.GetDamageBonus(attacker, playerDefender));

            for (var i = 0; i < 6; i++)
            {
                AddAggressed(attacker, CreateMobile(location: new Point3D(6205 + i, 500, 0)));
            }

            Assert.Equal(BattleLust.PvmDamageCap, BattleLust.GetDamageBonus(attacker, creatureDefender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Core._now = previousNow;
            BattleLust.Clear(attacker);
            weapon.Delete();
            DeleteAggressedDefenders(attacker);
            attacker.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void OnHit_AppliesBattleLustBeforeGlobalWeaponDamageCap()
    {
        var previousExpansion = Core.Expansion;
        var previousNow = Core._now;
        var attacker = CreateMobile(hitsMax: 5000, hits: 5000);
        var source = CreateMobile(location: new Point3D(6201, 500, 0));
        var defender = CreateMobile(location: new Point3D(6202, 500, 0));
        var weapon = EquipBattleLustWeapon(attacker);

        try
        {
            Core.Expansion = Expansion.SA;
            Core._now = TestNow;
            BuildBattleLustPoints(attacker, source, BattleLust.MaxPoints);
            AddAggressed(attacker, defender);

            var before = defender.Hits;
            weapon.OnHit(attacker, defender);
            Assert.Equal(115, before - defender.Hits);

            defender.Hits = before;
            weapon.OnHit(attacker, defender, 4.0);
            Assert.Equal(400, before - defender.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            Core._now = previousNow;
            BattleLust.Clear(attacker);
            weapon.Delete();
            DeleteAggressedDefenders(attacker);
            attacker.Delete();
            source.Delete();
        }
    }

    [Fact]
    public void RunicAttributeGeneration_DoesNotRollBattleLust()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            Core.Expansion = Expansion.SA;

            BaseRunicTool.ApplyAttributesTo(weapon, false, 0, 25, 100, 100);

            Assert.Equal(0, weapon.ExtendedWeaponAttributes.BattleLust);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    private static TestKatana EquipBattleLustWeapon(Mobile wielder)
    {
        var weapon = new TestKatana();
        weapon.ExtendedWeaponAttributes.BattleLust = 1;
        wielder.AddItem(weapon);
        return weapon;
    }

    private static void BuildBattleLustPoints(Mobile wielder, Mobile source, int points)
    {
        for (var i = 0; i < points; i++)
        {
            ApplyDamage(wielder, source, BattleLust.DamageThreshold);
            Core._now = Core._now.AddSeconds(2);
        }

        Assert.Equal(points, BattleLust.GetPoints(wielder));
    }

    private static void ApplyDamage(Mobile damaged, Mobile source, int damage) =>
        AOS.Damage(damaged, source, damage, false, 100, 0, 0, 0, 0);

    private static Mobile CreateMobile(
        int hitsMax = 500,
        int hits = 500,
        bool player = false,
        Point3D location = default
    )
    {
        var mobile = new Mobile(World.NewMobile)
        {
            Player = player
        };

        mobile.DefaultMobileInit();
        InitializeHits(mobile, hitsMax, hits);
        mobile.MoveToWorld(location == default ? new Point3D(6200, 500, 0) : location, Map.Felucca);
        return mobile;
    }

    private static PlayerMobile CreateDeadPlayerMobile(Point3D location)
    {
        var mobile = new PlayerMobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.Player = true;
        mobile.Hits = mobile.HitsMax;
        mobile.MoveToWorld(location, Map.Felucca);
        mobile.Body = mobile.Race.GhostBody(mobile);
        Assert.False(mobile.Alive);
        return mobile;
    }

    private static void InitializeHits(Mobile mobile, int hitsMax, int hits)
    {
        mobile.RawStr = Math.Max(1, (hitsMax - 50) * 2);
        Assert.Equal(hitsMax, mobile.HitsMax);
        mobile.Hits = hits;
    }

    private static void AddAggressed(Mobile attacker, Mobile defender) =>
        attacker.Aggressed.Add(AggressorInfo.Create(attacker, defender, false));

    private static void DeleteAggressedDefenders(Mobile attacker)
    {
        for (var i = 0; i < attacker.Aggressed.Count; i++)
        {
            var info = attacker.Aggressed[i];
            var defender = info.Defender;
            defender.Delete();
            info.Free();
        }

        attacker.Aggressed.Clear();
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
