using System;
using Server.Mobiles;
using Server.Spells.Necromancy;
using Xunit;

namespace Server.Tests.Spells.Necromancy;

[Collection("Sequential UOContent Tests")]
public class BloodOathSpellTests
{
    private static Mobile NewMobile()
    {
        var m = new Mobile(World.NewMobile);
        m.DefaultMobileInit();
        return m;
    }

    // Issue #1690: the real OSI duration is ((SpiritSpeak - Resist) / 8) + 8 seconds.
    // ModernUO previously used /80 (matching the bugged in-game tooltip), which made
    // Spirit Speak almost irrelevant to duration. RunUO/ServUO and the code's own
    // fixed-point comment both use /8.
    [Theory]
    [InlineData(120.0, 0.0, 23.0)]   // GM Spirit Speak, no resist
    [InlineData(120.0, 120.0, 8.0)]  // equal skills -> baseline 8s
    [InlineData(100.0, 20.0, 18.0)]  // (100-20)/8 + 8
    [InlineData(20.0, 0.0, 10.5)]    // minimum skill
    public void GetDurationSeconds_UsesDivideByEight(double ss, double resist, double expected)
    {
        Assert.Equal(expected, BloodOathSpell.GetDurationSeconds(ss, resist), 3);
    }

    // Player caster: Publish 48 resist mitigation does NOT apply; the attacker takes the
    // full reflected (original, un-bonused) damage.
    [Fact]
    public void ComputeReflectedDamage_NoMitigation_ReturnsOriginal()
    {
        Assert.Equal(40, BloodOathSpell.ComputeReflectedDamage(40, 120.0, applyResistMitigation: false));
    }

    // Creature caster (Publish 48 / SA+): ((Resist * 10) / 20) + 10 = % of reflected damage resisted.
    [Theory]
    [InlineData(40, 0.0, 36)]    // 10% resisted -> 40 * 0.90
    [InlineData(40, 100.0, 16)]  // 60% resisted -> 40 * 0.40
    [InlineData(40, 120.0, 12)]  // 70% resisted -> 40 * 0.30
    public void ComputeReflectedDamage_WithMitigation_ReducesByResist(int dmg, double resist, int expected)
    {
        Assert.Equal(expected, BloodOathSpell.ComputeReflectedDamage(dmg, resist, applyResistMitigation: true));
    }

    // The cursed target reflects to the caster; the caster attacking other mobiles must not reflect.
    [Fact]
    public void RegisterOath_BindsTargetToCaster_NotCasterToSelf()
    {
        var caster = NewMobile();
        var target = NewMobile();

        BloodOathSpell.RegisterOath(caster, target, TimeSpan.FromMinutes(5));

        Assert.Equal(caster, BloodOathSpell.GetBloodOath(target));
        Assert.Null(BloodOathSpell.GetBloodOath(caster));

        BloodOathSpell.RemoveCurse(target);
        caster.Delete();
        target.Delete();
    }

    // Death/delete of either party removes the oath, so RemoveCurse must resolve from either
    // the caster or the target key (the death hooks call it with `this`).
    [Fact]
    public void RemoveCurse_ByCaster_ClearsBothEntries()
    {
        var caster = NewMobile();
        var target = NewMobile();
        BloodOathSpell.RegisterOath(caster, target, TimeSpan.FromMinutes(5));

        Assert.True(BloodOathSpell.RemoveCurse(caster));

        Assert.Null(BloodOathSpell.GetBloodOath(target));
        Assert.False(BloodOathSpell.RemoveCurse(target)); // already removed

        caster.Delete();
        target.Delete();
    }

    [Fact]
    public void RemoveCurse_NotCursed_ReturnsFalse()
    {
        var m = NewMobile();
        Assert.False(BloodOathSpell.RemoveCurse(m));
        m.Delete();
    }

    // Issue #1690: death/delete of either party must break the oath immediately. The oath is wired
    // to the central PlayerMobile/BaseCreature death+delete events instead of a polling timer.
    [Fact]
    public void PlayerDeletedEvent_BreaksOath()
    {
        var caster = new PlayerMobile(World.NewMobile);
        caster.DefaultMobileInit();
        var target = new PlayerMobile(World.NewMobile);
        target.DefaultMobileInit();

        BloodOathSpell.RegisterOath(caster, target, TimeSpan.FromMinutes(5));

        PlayerMobile.PlayerDeletedEvent(caster); // central handler breaks the oath from the caster side

        Assert.Null(BloodOathSpell.GetBloodOath(target));
        Assert.False(BloodOathSpell.RemoveCurse(target));

        caster.Delete();
        target.Delete();
    }

    [Fact]
    public void CreatureDeletedEvent_BreaksOath()
    {
        var caster = new PlayerMobile(World.NewMobile);
        caster.DefaultMobileInit();
        var target = new TestCreature(World.NewMobile);
        target.DefaultMobileInit();

        BloodOathSpell.RegisterOath(caster, target, TimeSpan.FromMinutes(5));

        BaseCreature.CreatureDeletedEvent(target); // central handler breaks the oath from the target side

        Assert.Null(BloodOathSpell.GetBloodOath(target));
        Assert.False(BloodOathSpell.RemoveCurse(caster));

        caster.Delete();
        target.Delete();
    }

    private class TestCreature : BaseCreature
    {
        // Serial ctor skips AI/speed-table setup, which the test fixture does not configure.
        public TestCreature(Serial serial) : base(serial)
        {
        }
    }
}
