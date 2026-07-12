using System;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class ExtendedWeaponAttributesTests
{
    [Fact]
    public void AosWeaponAttribute_DoesNotExposeBaneOrBattleLust()
    {
        var names = Enum.GetNames<AosWeaponAttribute>();

        Assert.DoesNotContain("Bane", names);
        Assert.DoesNotContain("BattleLust", names);
    }

    [Fact]
    public void ExtendedWeaponAttribute_UsesFreshLowBits()
    {
        Assert.Equal(0x00000001, (int)ExtendedWeaponAttribute.Bane);
        Assert.Equal(0x00000002, (int)ExtendedWeaponAttribute.BattleLust);
        Assert.Equal(0x00000004, (int)ExtendedWeaponAttribute.HitSparks);
        Assert.Equal(0x00000008, (int)ExtendedWeaponAttribute.BloodDrinker);
        Assert.Equal(0x00000010, (int)ExtendedWeaponAttribute.HitSwarm);
        Assert.Equal(0x00000020, (int)ExtendedWeaponAttribute.SplinteringWeapon);
        Assert.Equal(0x00000040, (int)ExtendedWeaponAttribute.Focus);
        Assert.Equal(0x00000080, (int)ExtendedWeaponAttribute.BoneBreaker);
        Assert.Equal(0x00000100, (int)ExtendedWeaponAttribute.AssassinHoned);
        Assert.Equal(0x00000200, (int)ExtendedWeaponAttribute.Searing);
        Assert.Equal(0x00000400, (int)ExtendedWeaponAttribute.HitManaDrain);
    }

    [Fact]
    public void NewWeapon_DefaultsExtendedWeaponAttributesEmpty()
    {
        var weapon = new TestKatana();

        try
        {
            Assert.NotNull(weapon.ExtendedWeaponAttributes);
            Assert.True(weapon.ExtendedWeaponAttributes.IsEmpty);
            Assert.Equal(0, weapon.ExtendedWeaponAttributes.Bane);
            Assert.Equal(0, weapon.ExtendedWeaponAttributes.BattleLust);
            Assert.Equal(0, weapon.ExtendedWeaponAttributes.HitSparks);
            Assert.Equal(0, weapon.ExtendedWeaponAttributes.BloodDrinker);
            Assert.Equal(0, weapon.ExtendedWeaponAttributes.HitSwarm);
            Assert.Equal(0, weapon.ExtendedWeaponAttributes.SplinteringWeapon);
            Assert.Equal(0, weapon.ExtendedWeaponAttributes.Focus);
            Assert.Equal(0, weapon.ExtendedWeaponAttributes.BoneBreaker);
            Assert.Equal(0, weapon.ExtendedWeaponAttributes.AssassinHoned);
            Assert.Equal(0, weapon.ExtendedWeaponAttributes.Searing);
            Assert.Equal(0, weapon.ExtendedWeaponAttributes.HitManaDrain);
            Assert.False(weapon.SearingIgnited);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void StormCaller_UsesExtendedBattleLustProperty()
    {
        var weapon = new StormCaller();

        try
        {
            Assert.Equal(1, weapon.ExtendedWeaponAttributes.BattleLust);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void ExtendedWeaponAttributes_AreStaffEditableThroughCommandProperties()
    {
        var containerProperty = typeof(BaseWeapon).GetProperty(nameof(BaseWeapon.ExtendedWeaponAttributes));
        Assert.NotNull(containerProperty);
        var containerCommandProperty = containerProperty.GetCustomAttribute<CommandPropertyAttribute>();
        Assert.NotNull(containerCommandProperty);
        Assert.True(containerCommandProperty.CanModify);

        AssertStaffCommandProperty(nameof(ExtendedWeaponAttributes.Bane));
        AssertStaffCommandProperty(nameof(ExtendedWeaponAttributes.BattleLust));
        AssertStaffCommandProperty(nameof(ExtendedWeaponAttributes.HitSparks));
        AssertStaffCommandProperty(nameof(ExtendedWeaponAttributes.BloodDrinker));
        AssertStaffCommandProperty(nameof(ExtendedWeaponAttributes.HitSwarm));
        AssertStaffCommandProperty(nameof(ExtendedWeaponAttributes.SplinteringWeapon));
        AssertStaffCommandProperty(nameof(ExtendedWeaponAttributes.Focus));
        AssertStaffCommandProperty(nameof(ExtendedWeaponAttributes.BoneBreaker));
        AssertStaffCommandProperty(nameof(ExtendedWeaponAttributes.AssassinHoned));
        AssertStaffCommandProperty(nameof(ExtendedWeaponAttributes.Searing));
        AssertStaffCommandProperty(nameof(ExtendedWeaponAttributes.HitManaDrain));
    }

    [Fact]
    public void ExtendedWeaponAttributes_PersistThroughBaseWeaponSerialization()
    {
        var previousExpansion = Core.Expansion;
        var weapon = new TestKatana();
        var deserialized = new TestKatana();

        try
        {
            Core.Expansion = Expansion.HS;
            weapon.ExtendedWeaponAttributes.Bane = 1;
            weapon.ExtendedWeaponAttributes.BattleLust = 1;
            weapon.ExtendedWeaponAttributes.HitSparks = 20;
            weapon.ExtendedWeaponAttributes.BloodDrinker = 1;
            weapon.ExtendedWeaponAttributes.HitSwarm = 30;
            weapon.ExtendedWeaponAttributes.SplinteringWeapon = 25;
            weapon.ExtendedWeaponAttributes.Focus = 1;
            weapon.ExtendedWeaponAttributes.BoneBreaker = 1;
            weapon.ExtendedWeaponAttributes.AssassinHoned = 1;
            weapon.ExtendedWeaponAttributes.Searing = 1;
            weapon.ExtendedWeaponAttributes.HitManaDrain = 70;
            weapon.SearingIgnited = true;

            var writer = new BufferWriter(true);
            weapon.Serialize(writer);
            var buffer = new byte[writer.Position];
            writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

            var reader = new BufferReader(buffer);
            deserialized.Deserialize(reader);

            Assert.Equal(buffer.Length, reader.Position);
            Assert.Equal(1, deserialized.ExtendedWeaponAttributes.Bane);
            Assert.Equal(1, deserialized.ExtendedWeaponAttributes.BattleLust);
            Assert.Equal(20, deserialized.ExtendedWeaponAttributes.HitSparks);
            Assert.Equal(1, deserialized.ExtendedWeaponAttributes.BloodDrinker);
            Assert.Equal(30, deserialized.ExtendedWeaponAttributes.HitSwarm);
            Assert.Equal(25, deserialized.ExtendedWeaponAttributes.SplinteringWeapon);
            Assert.Equal(1, deserialized.ExtendedWeaponAttributes.Focus);
            Assert.Equal(1, deserialized.ExtendedWeaponAttributes.BoneBreaker);
            Assert.Equal(1, deserialized.ExtendedWeaponAttributes.AssassinHoned);
            Assert.Equal(1, deserialized.ExtendedWeaponAttributes.Searing);
            Assert.Equal(70, deserialized.ExtendedWeaponAttributes.HitManaDrain);
            Assert.True(deserialized.SearingIgnited);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            weapon.Delete();
            deserialized.Delete();
        }
    }

    private static void AssertStaffCommandProperty(string propertyName)
    {
        var property = typeof(ExtendedWeaponAttributes).GetProperty(propertyName);
        Assert.NotNull(property);

        var attribute = property.GetCustomAttribute<CommandPropertyAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(AccessLevel.GameMaster, attribute.ReadLevel);
        Assert.Equal(AccessLevel.GameMaster, attribute.WriteLevel);
    }

    private class TestKatana : Katana
    {
        public override int ComputeDamage(Mobile attacker, Mobile defender) => 1;
        public override int AbsorbDamage(Mobile attacker, Mobile defender, int damage) => damage;
        public override void AddBlood(Mobile attacker, Mobile defender, int damage)
        {
        }
    }
}
