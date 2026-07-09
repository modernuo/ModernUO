using System;
using System.Collections.Generic;
using System.Globalization;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BloodDrinkerPropertyTests
{
    private const int BloodDrinkerCliloc = 1113591;
    private static readonly Point3D TestLocation = new(6200, 500, 0);

    [Fact]
    public void ExtendedWeaponAttributes_StoresAndDupesBloodDrinker()
    {
        var weapon = new TestKatana();
        var dupe = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.BloodDrinker = 1;

            weapon.Dupe(dupe);

            Assert.Equal(1, weapon.ExtendedWeaponAttributes.BloodDrinker);
            Assert.Equal(1, dupe.ExtendedWeaponAttributes.BloodDrinker);
        }
        finally
        {
            weapon.Delete();
            dupe.Delete();
        }
    }

    [Fact]
    public void BaseWeapon_GetProperties_GatesBloodDrinkerTooltipToStygianAbyss()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.BloodDrinker = 1;

            Core.Expansion = Expansion.ML;
            var preStygianAbyss = new RecordingPropertyList();
            weapon.GetProperties(preStygianAbyss);
            Assert.DoesNotContain(preStygianAbyss.Entries, entry => entry.Number == BloodDrinkerCliloc);

            Core.Expansion = Expansion.SA;
            var stygianAbyss = new RecordingPropertyList();
            weapon.GetProperties(stygianAbyss);
            Assert.Contains(stygianAbyss.Entries, entry => entry.Number == BloodDrinkerCliloc);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    [Fact]
    public void CheckBloodDrinker_RequiresStygianAbyssLivingAttackerAndEquippedProperty()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile(hitsMax: 200, hits: 100);
        var weapon = EquipBloodDrinkerWeapon(attacker);
        var deadAttacker = CreateMobile(hitsMax: 200, hits: 100);
        var deadWeapon = EquipBloodDrinkerWeapon(deadAttacker);
        var deletedAttacker = CreateMobile(hitsMax: 200, hits: 100);
        var deletedWeapon = EquipBloodDrinkerWeapon(deletedAttacker);

        try
        {
            Core.Expansion = Expansion.SA;
            Assert.True(BleedAttack.CheckBloodDrinker(attacker));

            weapon.ExtendedWeaponAttributes.BloodDrinker = 0;
            Assert.False(BleedAttack.CheckBloodDrinker(attacker));

            weapon.ExtendedWeaponAttributes.BloodDrinker = 1;
            Core.Expansion = Expansion.ML;
            Assert.False(BleedAttack.CheckBloodDrinker(attacker));

            Core.Expansion = Expansion.SA;
            deadAttacker.Kill();
            Assert.False(BleedAttack.CheckBloodDrinker(deadAttacker));

            deletedAttacker.Delete();
            Assert.False(BleedAttack.CheckBloodDrinker(deletedAttacker));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            deadWeapon.Delete();
            deletedWeapon.Delete();
            attacker.Delete();
            deadAttacker.Delete();
            deletedAttacker.Delete();
        }
    }

    [Fact]
    public void DoBleed_WithBloodDrinker_HealsAttackerForAppliedPlayerDamage()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var attacker = CreateMobile(hitsMax: 200, hits: 100);
        var defender = CreateMobile(hitsMax: 500, hits: 500, player: true, location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.SA;

            BleedAttack.DoBleed(defender, attacker, 5, bloodDrinker: true);

            Assert.Equal(495, defender.Hits);
            Assert.Equal(105, attacker.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void DoBleed_NonPlayerDefender_HealsForAppliedPvMBleedDamage()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var attacker = CreateMobile(hitsMax: 300, hits: 150);
        var defender = CreateMobile(hitsMax: 500, hits: 500, location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.SA;

            BleedAttack.DoBleed(defender, attacker, 5, bloodDrinker: true);

            Assert.Equal(490, defender.Hits);
            Assert.Equal(160, attacker.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void DoBleed_WithoutBloodDrinker_DoesNotHealAttacker()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var attacker = CreateMobile(hitsMax: 200, hits: 100);
        var defender = CreateMobile(hitsMax: 500, hits: 500, player: true, location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.SA;

            BleedAttack.DoBleed(defender, attacker, 5);

            Assert.Equal(495, defender.Hits);
            Assert.Equal(100, attacker.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void DoBleed_WithBloodDrinker_RespectsHitsMaxCap()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var attacker = CreateMobile(hitsMax: 200, hits: 199);
        var defender = CreateMobile(hitsMax: 500, hits: 500, player: true, location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.SA;

            BleedAttack.DoBleed(defender, attacker, 5, bloodDrinker: true);

            Assert.Equal(495, defender.Hits);
            Assert.Equal(200, attacker.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void DoBleed_WithDeadAttacker_DoesNotHeal()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        var attacker = CreateMobile(hitsMax: 200, hits: 100);
        var defender = CreateMobile(hitsMax: 500, hits: 500, player: true, location: new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.SA;
            attacker.Kill();
            Assert.False(attacker.Alive);

            BleedAttack.DoBleed(defender, attacker, 5, bloodDrinker: true);

            Assert.Equal(495, defender.Hits);
            Assert.Equal(0, attacker.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_BleedImmuneTarget_DoesNotStartBleedContext()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile(hitsMax: 200, hits: 100, mana: 100);
        var defender = new TestBleedImmuneCreature();
        var weapon = EquipBloodDrinkerWeapon(attacker);
        var ability = new BleedAttack();

        try
        {
            Core.Expansion = Expansion.SA;
            defender.DefaultMobileInit();
            defender.MoveToWorld(new Point3D(6201, 500, 0), Map.Felucca);

            ability.OnHit(attacker, defender, 1, new WorldLocation(defender));

            Assert.False(BleedAttack.IsBleeding(defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            BleedAttack.EndBleed(defender, false);
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_FailedManaCheck_DoesNotStartBleedContext()
    {
        var previousExpansion = Core.Expansion;
        var attacker = CreateMobile(hitsMax: 200, hits: 100, mana: 0);
        var defender = CreateMobile(hitsMax: 500, hits: 500, location: new Point3D(6201, 500, 0));
        var weapon = EquipBloodDrinkerWeapon(attacker);
        var ability = new BleedAttack();

        try
        {
            Core.Expansion = Expansion.SA;

            ability.OnHit(attacker, defender, 1, new WorldLocation(defender));

            Assert.False(BleedAttack.IsBleeding(defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            BleedAttack.EndBleed(defender, false);
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void RunicAttributeGeneration_DoesNotRollBloodDrinker()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            Core.Expansion = Expansion.SA;

            BaseRunicTool.ApplyAttributesTo(weapon, false, 0, 25, 100, 100);

            Assert.Equal(0, weapon.ExtendedWeaponAttributes.BloodDrinker);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    private static TestKatana EquipBloodDrinkerWeapon(Mobile wielder)
    {
        var weapon = new TestKatana();
        weapon.ExtendedWeaponAttributes.BloodDrinker = 1;
        wielder.AddItem(weapon);
        return weapon;
    }

    private static Mobile CreateMobile(
        int hitsMax,
        int hits,
        int manaMax = 100,
        int mana = 100,
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

    private class TestKatana : Katana
    {
        public override int ComputeDamage(Mobile attacker, Mobile defender) => 1;
        public override int AbsorbDamage(Mobile attacker, Mobile defender, int damage) => damage;
        public override void AddBlood(Mobile attacker, Mobile defender, int damage)
        {
        }
    }

    private class TestBleedImmuneCreature : BaseCreature
    {
        public TestBleedImmuneCreature() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
        {
            Body = 0xC8;
        }

        public override bool BleedImmune => true;

        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.2;
            passiveSpeed = 0.4;
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
