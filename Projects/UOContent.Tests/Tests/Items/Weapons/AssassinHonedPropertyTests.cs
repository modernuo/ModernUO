using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.Text;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class AssassinHonedPropertyTests
{
    private const int AssassinHonedCliloc = 1152206;

    [Fact]
    public void ExtendedWeaponAttributes_StoresAndDupesAssassinHoned()
    {
        var weapon = new TestKatana();
        var dupe = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.AssassinHoned = 1;
            weapon.Dupe(dupe);

            Assert.Equal(1, weapon.ExtendedWeaponAttributes.AssassinHoned);
            Assert.Equal(1, dupe.ExtendedWeaponAttributes.AssassinHoned);
        }
        finally
        {
            weapon.Delete();
            dupe.Delete();
        }
    }

    [Fact]
    public void ExtendedWeaponAttributes_GetProperties_GatesAssassinHonedTooltipToHighSeas()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();

        try
        {
            weapon.ExtendedWeaponAttributes.AssassinHoned = 1;

            Core.Expansion = Expansion.ML;
            var preHighSeas = new RecordingPropertyList();
            weapon.ExtendedWeaponAttributes.GetProperties(preHighSeas);
            Assert.DoesNotContain(preHighSeas.Numbers, number => number == AssassinHonedCliloc);

            Core.Expansion = Expansion.HS;
            var highSeas = new RecordingPropertyList();
            weapon.ExtendedWeaponAttributes.GetProperties(highSeas);
            Assert.Contains(highSeas.Numbers, number => number == AssassinHonedCliloc);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
        }
    }

    [Theory]
    [InlineData(2.0f, 73)]
    [InlineData(4.0f, 36)]
    public void GetAssassinHonedDamageBonus_UsesOriginalMlSpeed(float mlSpeed, int expectedBonus)
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana(mlSpeed);
        var attacker = CreateMobile(Direction.East | Direction.Running);
        var defender = CreateMobile(Direction.East);

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.ExtendedWeaponAttributes.AssassinHoned = 1;
            weapon.Attributes.WeaponSpeed = 60;

            Assert.Equal(expectedBonus, weapon.GetAssassinHonedDamageBonus(attacker, defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void GetAssassinHonedDamageBonus_RequiresHighSeasAndRearFacingRelation()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile(Direction.East);
        var defender = CreateMobile(Direction.East);

        try
        {
            weapon.ExtendedWeaponAttributes.AssassinHoned = 1;

            Core.Expansion = Expansion.ML;
            Assert.Equal(0, weapon.GetAssassinHonedDamageBonus(attacker, defender));

            Core.Expansion = Expansion.HS;
            defender.Direction = Direction.West;
            Assert.Equal(0, weapon.GetAssassinHonedDamageBonus(attacker, defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Theory]
    [InlineData(0, 73)]
    [InlineData(1, 0)]
    public void GetAssassinHonedDamageBonus_RangedWeaponsUseIndependentHalfChance(int randomValue, int expectedBonus)
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestBow();
        var attacker = CreateMobile(Direction.East);
        var defender = CreateMobile(Direction.East);

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.ExtendedWeaponAttributes.AssassinHoned = 1;

            using var random = new PredictableRandom(randomValue);
            Assert.Equal(expectedBonus, weapon.GetAssassinHonedDamageBonus(attacker, defender));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_AssassinHonedUsesTheExistingThreeHundredPercentDamageCap()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var attacker = CreateMobile(Direction.East);
        var defender = CreateMobile(Direction.East);

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.ExtendedWeaponAttributes.AssassinHoned = 1;
            var before = defender.Hits;

            weapon.OnHit(attacker, defender, 4.0);

            Assert.Equal(400, before - defender.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnSwing_MissDoesNotApplyAssassinHoned()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana { Hit = false };
        var attacker = CreateMobile(Direction.East, Map.Felucca, new Point3D(6200, 500, 0));
        var defender = CreateMobile(Direction.East, Map.Felucca, new Point3D(6201, 500, 0));

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.ExtendedWeaponAttributes.AssassinHoned = 1;
            var before = defender.Hits;

            weapon.OnSwing(attacker, defender);

            Assert.Equal(before, defender.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void OnHit_ZeroDamageAfterAbsorptionDoesNotApplyAssassinHoned()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana { AbsorbAllDamage = true };
        var attacker = CreateMobile(Direction.East);
        var defender = CreateMobile(Direction.East);

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.ExtendedWeaponAttributes.AssassinHoned = 1;
            var before = defender.Hits;

            weapon.OnHit(attacker, defender);

            Assert.Equal(before, defender.Hits);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            attacker.Delete();
            defender.Delete();
        }
    }

    private static Mobile CreateMobile(Direction direction, Map map = null, Point3D location = default)
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.RawStr = 1000;
        mobile.Hits = mobile.HitsMax;
        mobile.Direction = direction;

        if (map != null)
        {
            mobile.MoveToWorld(location, map);
        }

        return mobile;
    }

    private sealed class TestKatana(float mlSpeed = 2.0f) : Katana
    {
        public bool AbsorbAllDamage { get; init; }
        public bool Hit { get; init; } = true;

        public override float MlSpeed => mlSpeed;
        public override bool CheckHit(Mobile attacker, Mobile defender) => Hit;
        public override int ComputeDamage(Mobile attacker, Mobile defender) => 100;
        public override int AbsorbDamage(Mobile attacker, Mobile defender, int damage) => AbsorbAllDamage ? 0 : damage;

        public override void AddBlood(Mobile attacker, Mobile defender, int damage)
        {
        }
    }

    private sealed class TestBow : Bow
    {
        public override float MlSpeed => 2.0f;
    }

    private sealed class RecordingPropertyList : IPropertyList
    {
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

        public void InitializeInterpolation(int literalLength, int formattedCount)
        {
        }

        public void AppendLiteral(string value)
        {
        }

        public void AppendFormatted<T>(T value)
        {
        }

        public void AppendFormatted<T>(T value, string format)
        {
        }

        public void AppendFormatted<T>(T value, int alignment)
        {
        }

        public void AppendFormatted<T>(T value, int alignment, string format)
        {
        }

        public void AppendFormatted(ReadOnlySpan<char> value)
        {
        }

        public void AppendFormatted(ReadOnlySpan<char> value, int alignment, string format = null)
        {
        }

        public void AppendFormatted(object value, int alignment = 0, string format = null)
        {
        }

        public void AppendFormatted(string value)
        {
        }

        public void AppendFormatted(string value, int alignment, string format = null)
        {
        }
    }
}
