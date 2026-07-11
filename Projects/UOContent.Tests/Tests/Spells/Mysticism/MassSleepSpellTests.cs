using System;
using System.Collections.Generic;
using System.Reflection;
using Server.Items;
using Server.Spells;
using Server.Spells.Mysticism;
using Xunit;

namespace Server.Tests.Spells.Mysticism;

[Collection("Sequential UOContent Tests")]
public class MassSleepSpellTests
{
    [Fact]
    public void SpellMetadataAndScroll_MatchFifthCircleMysticismSources()
    {
        var caster = NewMysticCaster();
        var spell = new MassSleepSpell(caster);
        var scroll = new MassSleepScroll();

        try
        {
            Assert.Equal("Mass Sleep", spell.Name);
            Assert.Equal("Vas Zu", spell.Mantra);
            Assert.Equal(SpellCircle.Fifth, spell.Circle);
            Assert.Equal(TimeSpan.FromSeconds(1.5), spell.CastDelayBase);
            Assert.Equal(14, spell.GetMana());
            Assert.Equal(45.0, spell.RequiredSkill);
            Assert.Equal([Reagent.Ginseng, Reagent.Nightshade, Reagent.SpidersSilk], spell.Reagents);
            Assert.Equal(686, GetSpellScrollId(scroll));
        }
        finally
        {
            caster.Delete();
            scroll.Delete();
        }
    }

    [Fact]
    public void RegisterMysticism_RegistersMassSleepOnlyAtSa()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewMysticCaster();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.ML;
            Initializer.Configure();
            Assert.Null(SpellRegistry.NewSpell(686, caster, null));

            ResetSpellRegistry();
            Core.Expansion = Expansion.SA;
            Initializer.Configure();
            Assert.Same(typeof(MassSleepSpell), SpellRegistry.Types[686]);
            Assert.IsType<MassSleepSpell>(SpellRegistry.NewSpell(686, caster, null));
        }
        finally
        {
            Core.Expansion = previousExpansion;
            ResetSpellRegistry();
            Initializer.Configure();
            caster.Delete();
        }
    }

    [Fact]
    public void SleepDuration_UsesBetterSupportSkillAndTargetResist()
    {
        var caster = NewMysticCaster();
        var target = NewMobile();

        try
        {
            caster.Skills.Mysticism.Base = 120.0;
            caster.Skills.Focus.Base = 80.0;
            caster.Skills.Imbuing.Base = 120.0;
            target.Skills.MagicResist.Base = 50.0;

            Assert.Equal(TimeSpan.FromSeconds(10), SleepSpell.GetDuration(caster, target));

            target.Skills.MagicResist.Base = 150.0;
            Assert.Equal(TimeSpan.Zero, SleepSpell.GetDuration(caster, target));
        }
        finally
        {
            caster.Delete();
            target.Delete();
        }
    }

    [Fact]
    public void DamageBreaksSleepAndAppliesResistScaledPlayerImmunity()
    {
        var caster = NewMysticCaster();
        var target = NewMobile();
        target.Player = true;
        target.Skills.MagicResist.Base = 120.0;

        try
        {
            Assert.True(SleepSpell.Apply(caster, target, TimeSpan.FromSeconds(10)));
            Assert.True(SleepSpell.IsUnderEffect(target));
            Assert.Equal(SleepSpell.CastSpeedMalus, SleepSpell.GetCastSpeedMalus(target));
            Assert.Equal(SleepSpell.CastRecoveryMalus, SleepSpell.GetCastRecoveryMalus(target));
            Assert.Equal(SleepSpell.SwingSpeedMalus, SleepSpell.GetSwingSpeedMalus(target));
            Assert.Equal(TimeSpan.FromSeconds(12), SleepSpell.GetImmunityDuration(target));

            SleepSpell.OnMobileDamaged(target, 1);

            Assert.False(SleepSpell.IsUnderEffect(target));
            Assert.True(SleepSpell.IsImmune(target));
            Assert.False(SleepSpell.Apply(caster, target, TimeSpan.FromSeconds(10)));
        }
        finally
        {
            SleepSpell.ClearAllForTests();
            caster.Delete();
            target.Delete();
        }
    }

    private static Mobile NewMobile()
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.InitStats(100, 100, 100);
        mobile.Hits = mobile.HitsMax;
        mobile.Mana = mobile.ManaMax;
        return mobile;
    }

    private static Mobile NewMysticCaster()
    {
        var caster = NewMobile();
        caster.Skills.Mysticism.Base = 120.0;
        caster.Skills.Focus.Base = 120.0;
        return caster;
    }

    private static int GetSpellScrollId(SpellScroll scroll)
    {
        var field = typeof(SpellScroll).GetField("_spellID", BindingFlags.Instance | BindingFlags.NonPublic);
        return (int)field!.GetValue(scroll)!;
    }

    private static void ResetSpellRegistry()
    {
        var types = (Type[])typeof(SpellRegistry)
            .GetField("m_Types", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
        Array.Clear(types);

        var idsFromTypes = (Dictionary<Type, int>)typeof(SpellRegistry)
            .GetField("m_IDsFromTypes", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
        idsFromTypes.Clear();

        typeof(SpellRegistry)
            .GetField("m_Count", BindingFlags.Static | BindingFlags.NonPublic)!
            .SetValue(null, 0);

        SpellRegistry.SpecialMoves.Clear();
    }
}
