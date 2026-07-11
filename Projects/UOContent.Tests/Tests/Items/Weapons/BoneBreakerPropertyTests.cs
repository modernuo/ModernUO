using System;
using System.Collections.Generic;
using System.Globalization;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BoneBreakerPropertyTests
{
    private const int BoneBreakerCliloc = 1157318;
    private static readonly Point3D TestLocation = new(6200, 520, 0);

    [Fact]
    public void ExtendedWeaponAttributes_StoresAndDupesBoneBreaker()
    {
        var weapon = new TestKatana();
        var dupe = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.BoneBreaker = 1;

            weapon.Dupe(dupe);

            Assert.Equal(1, weapon.ExtendedWeaponAttributes.BoneBreaker);
            Assert.Equal(1, dupe.ExtendedWeaponAttributes.BoneBreaker);
        }
        finally
        {
            weapon.Delete();
            dupe.Delete();
        }
    }

    [Fact]
    public void ExtendedWeaponAttributes_GetProperties_GatesBoneBreakerTooltipToTimeOfLegends()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.BoneBreaker = 1;

            Core.Expansion = Expansion.HS;
            var highSeas = new RecordingPropertyList();
            weapon.ExtendedWeaponAttributes.GetProperties(highSeas);
            Assert.DoesNotContain(highSeas.Entries, entry => entry.Number == BoneBreakerCliloc);

            Core.Expansion = Expansion.TOL;
            var timeOfLegends = new RecordingPropertyList();
            weapon.ExtendedWeaponAttributes.GetProperties(timeOfLegends);
            Assert.Contains(timeOfLegends.Entries, entry => entry.Number == BoneBreakerCliloc);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    [Fact]
    public void OnHit_NormalHitAppliesIndependentManaBonus()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile(mana: BoneBreaker.ManaThreshold);
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            weapon.ExtendedWeaponAttributes.BoneBreaker = 1;

            weapon.OnHit(attacker, defender);

            Assert.Equal(0, attacker.Mana);
            Assert.Equal(149, defender.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            BoneBreaker.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(20, 24)]
    [InlineData(40, 18)]
    [InlineData(75, 18)]
    public void TryProcOnNormalHit_ScalesManaCostWithLowerManaCost(int lowerManaCost, int expectedCost)
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile(mana: BoneBreaker.ManaThreshold);
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            weapon.Attributes.LowerManaCost = lowerManaCost;
            Assert.True(attacker.EquipItem(weapon));

            BoneBreaker.TryProcOnNormalHit(attacker, defender, 1, 0);

            Assert.Equal(BoneBreaker.ManaThreshold - expectedCost, attacker.Mana);
            Assert.Equal(150, defender.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            BoneBreaker.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void TryProcOnNormalHit_BelowManaThresholdCanDrainButDoesNotDamageOrSpendMana()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile(mana: BoneBreaker.ManaThreshold - 1);
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;

            BoneBreaker.TryProcOnNormalHit(attacker, defender, 1, 100);

            Assert.Equal(BoneBreaker.ManaThreshold - 1, attacker.Mana);
            Assert.Equal(200, defender.Hits);
            Assert.True(BoneBreaker.HasActiveDrain(defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            BoneBreaker.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Theory]
    [InlineData(typeof(TestWeaponAbility))]
    [InlineData(typeof(TestSpecialMove))]
    public void OnHit_WeaponAbilitiesAndSpecialMovesDoNotTriggerBoneBreaker(Type actionType)
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile(mana: BoneBreaker.ManaThreshold);
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            weapon.ExtendedWeaponAttributes.BoneBreaker = 1;

            if (typeof(WeaponAbility).IsAssignableFrom(actionType))
            {
                WeaponAbility.Table[attacker] = (WeaponAbility)Activator.CreateInstance(actionType);
            }
            else
            {
                SpecialMove.Table[attacker] = (SpecialMove)Activator.CreateInstance(actionType);
            }

            weapon.OnHit(attacker, defender);

            Assert.Equal(BoneBreaker.ManaThreshold, attacker.Mana);
            Assert.False(BoneBreaker.HasActiveDrain(defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            WeaponAbility.Table.Remove(attacker);
            SpecialMove.Table.Remove(attacker);
            BoneBreaker.ClearAll();
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void Drain_BlocksOnlyRefreshPotionsUntilTheFourTicksComplete()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));
        var refresh = new RefreshPotion();
        var totalRefresh = new TotalRefreshPotion();

        try
        {
            Core.Expansion = Expansion.TOL;
            defender.Stam = defender.StamMax - 1;
            refresh.MoveToWorld(defender.Location, defender.Map);
            totalRefresh.MoveToWorld(defender.Location, defender.Map);
            BoneBreaker.TryProcOnNormalHit(attacker, defender, 1, 100);

            Assert.True(BoneBreaker.HasActiveDrain(defender));
            Assert.False(refresh.CanDrink(defender));
            Assert.False(totalRefresh.CanDrink(defender));

            for (var i = 0; i < BoneBreaker.DrainTickCount; i++)
            {
                Assert.True(BoneBreaker.TickForTests(defender));
            }

            Assert.False(BoneBreaker.HasActiveDrain(defender));
            Assert.True(BoneBreaker.IsImmune(defender));
            Assert.True(refresh.CanDrink(defender));
            Assert.True(totalRefresh.CanDrink(defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            BoneBreaker.ClearAll();
            refresh.Delete();
            totalRefresh.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void Drain_UsesFourOneSecondTicksAndPreservesOneStamina()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            defender.Stam = 2;
            BoneBreaker.TryProcOnNormalHit(attacker, defender, 1, 100);

            Assert.Equal(TimeSpan.FromSeconds(1.0), BoneBreaker.DrainTickInterval);

            for (var i = 0; i < BoneBreaker.DrainTickCount; i++)
            {
                Assert.True(BoneBreaker.TickForTests(defender));
                Assert.Equal(1, defender.Stam);
            }

            Assert.False(BoneBreaker.HasActiveDrain(defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            BoneBreaker.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void ImmunityAfterDrainExpires_SuppressesBothBranchesWithoutManaCost()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile(mana: 100);
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            BoneBreaker.TryProcOnNormalHit(attacker, defender, 1, 100);

            for (var i = 0; i < BoneBreaker.DrainTickCount; i++)
            {
                BoneBreaker.TickForTests(defender);
            }

            var hitsAfterFirstProc = defender.Hits;
            attacker.Mana = 100;

            BoneBreaker.TryProcOnNormalHit(attacker, defender, 1, 100);

            Assert.True(BoneBreaker.IsImmune(defender));
            Assert.False(BoneBreaker.HasActiveDrain(defender));
            Assert.Equal(100, attacker.Mana);
            Assert.Equal(hitsAfterFirstProc, defender.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            BoneBreaker.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void ExpiredImmunity_AllowsBoneBreakerToProcAgain()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile(mana: 100);
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            Assert.Equal(TimeSpan.FromSeconds(60), BoneBreaker.ImmunityDuration);
            BoneBreaker.TryProcOnNormalHit(attacker, defender, 1, 100);

            for (var i = 0; i < BoneBreaker.DrainTickCount; i++)
            {
                BoneBreaker.TickForTests(defender);
            }

            BoneBreaker.ExpireImmunityForTests(defender);
            attacker.Mana = 100;
            var hitsBeforeSecondProc = defender.Hits;

            BoneBreaker.TryProcOnNormalHit(attacker, defender, 1, 100);

            Assert.False(BoneBreaker.IsImmune(defender));
            Assert.True(BoneBreaker.HasActiveDrain(defender));
            Assert.Equal(70, attacker.Mana);
            Assert.Equal(hitsBeforeSecondProc - BoneBreaker.Damage, defender.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            BoneBreaker.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void DrainCleanup_ReleasesStateWhenTargetLeavesTheWorld()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            BoneBreaker.Configure();
            Core.Expansion = Expansion.TOL;
            var map = defender.Map;
            BoneBreaker.TryProcOnNormalHit(attacker, defender, 1, 100);
            defender.Internalize();
            defender.MoveToWorld(new Point3D(6201, 520, 0), map);

            Assert.False(BoneBreaker.HasActiveDrain(defender));
            Assert.Equal(0, BoneBreaker.ActiveDrainCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            BoneBreaker.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void DrainCleanup_ReleasesStateWhenAttackerDies()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile();
        var defender = CreateMobile(location: new Point3D(6201, 520, 0));

        try
        {
            Core.Expansion = Expansion.TOL;
            BoneBreaker.TryProcOnNormalHit(attacker, defender, 1, 100);

            BoneBreaker.Clear(attacker);

            Assert.False(BoneBreaker.HasActiveDrain(defender));
            Assert.Equal(0, BoneBreaker.ActiveDrainCount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            BoneBreaker.ClearAll();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void RunicAttributeGeneration_DoesNotRollBoneBreaker()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            Core.Expansion = Expansion.TOL;

            BaseRunicTool.ApplyAttributesTo(weapon, false, 0, 25, 100, 100);

            Assert.Equal(0, weapon.ExtendedWeaponAttributes.BoneBreaker);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    private static Mobile CreateMobile(int mana = 0, Point3D location = default)
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.RawStr = 300;
        mobile.RawDex = 100;
        mobile.RawInt = 100;
        mobile.Hits = mobile.HitsMax;
        mobile.Stam = mobile.StamMax;
        mobile.Mana = mana;
        mobile.MoveToWorld(location == default ? TestLocation : location, Map.Felucca);
        return mobile;
    }

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
