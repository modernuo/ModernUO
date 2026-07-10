using System;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Spellweaving;
using Xunit;

namespace Server.Tests.Spells.Spellweaving;

[Collection("Sequential UOContent Tests")]
public class ArcaneEmpowermentSpellTests
{
    [Fact]
    public void SpellMetadata_MatchesArcaneEmpowermentSources()
    {
        var caster = NewCaster();
        var spell = new ArcaneEmpowermentSpell(caster);

        Assert.Equal("Arcane Empowerment", spell.Name);
        Assert.Equal("Aslavdra", spell.Mantra);
        Assert.Equal(TimeSpan.FromSeconds(4.0), spell.CastDelayBase);
        Assert.Equal(24.0, spell.RequiredSkill);
        Assert.Equal(50, spell.GetMana());
        Assert.Equal(SkillName.Spellweaving, spell.CastSkill);
        Assert.Equal(SkillName.Spellweaving, spell.DamageSkill);

        caster.Delete();
    }

    [Fact]
    public void Formula_UsesSkillAndFocusForDurationAndBonuses()
    {
        Assert.Equal(TimeSpan.FromSeconds(20), ArcaneEmpowermentSpell.GetDuration(1200, 0));
        Assert.Equal(TimeSpan.FromSeconds(24), ArcaneEmpowermentSpell.GetDuration(1200, 2));
        Assert.Equal(10, ArcaneEmpowermentSpell.GetBaseBonus(1200));
        Assert.Equal(20, ArcaneEmpowermentSpell.GetSpellDamageBonus(1200, 2, false));
        Assert.Equal(12, ArcaneEmpowermentSpell.GetSpellDamageBonus(1200, 2, true));
        Assert.Equal(20, ArcaneEmpowermentSpell.GetHealingBonus(1200, 2));
        Assert.Equal(20, ArcaneEmpowermentSpell.GetDispelDifficultyBonus(2));
    }

    [Fact]
    public void SpellweavingBookAndScroll_UseArcaneEmpowermentSlot()
    {
        var book = new SpellweavingBook(1UL << (615 - 600));
        var scroll = new ArcaneEmpowermentScroll();

        Assert.Equal(600, book.BookOffset);
        Assert.Equal(16, book.BookCount);
        Assert.True(book.HasSpell(615));
        Assert.Equal(615, GetSpellScrollId(scroll));

        book.Delete();
        scroll.Delete();
    }

    [Fact]
    public void CheckCast_EnforcesRequiredSkillAndMana()
    {
        var caster = NewCaster(23.9);
        var spell = new ArcaneEmpowermentSpell(caster);

        caster.Mana = 100;
        Assert.False(spell.CheckCast());

        caster.Skills.Spellweaving.Base = 24.0;
        caster.Mana = 49;
        Assert.False(spell.CheckCast());

        caster.Mana = 100;
        Assert.True(spell.CheckCast());

        caster.Delete();
    }

    [Fact]
    public void RegisterSpellweaving_PreMlDoesNotExposeArcaneEmpowerment()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.SE;
            Initializer.Configure();

            Assert.Null(SpellRegistry.NewSpell(615, caster, null));
            Assert.Equal(-1, SpellRegistry.GetRegistryNumber(typeof(ArcaneEmpowermentSpell)));
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
    public void RegisterSpellweaving_MlExposesArcaneEmpowermentAtSpellId615()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.ML;
            Initializer.Configure();

            Assert.Same(typeof(ArcaneEmpowermentSpell), SpellRegistry.Types[615]);
            Assert.Equal(615, SpellRegistry.GetRegistryNumber(typeof(ArcaneEmpowermentSpell)));
            Assert.IsType<ArcaneEmpowermentSpell>(SpellRegistry.NewSpell(615, caster, null));
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
    public void Effect_AppliesDamageHealingSummonHealthAndDispelBonusesWithoutStacking()
    {
        var caster = NewCaster();
        var target = NewTarget();
        var follower = new TestSummon
        {
            HitsMaxSeed = 100,
            Summoned = true,
            SummonMaster = caster
        };
        follower.Hits = 100;

        ArcaneEmpowermentSpell.ApplyEffectForTests(caster, 1200, 2);

        Assert.True(ArcaneEmpowermentSpell.IsUnderEffects(caster));
        Assert.Equal(110, follower.HitsMax);
        Assert.Equal(110, follower.Hits);

        var damage = 100;
        follower.AlterMeleeDamageTo(target, ref damage);
        Assert.Equal(120, damage);

        var spellDamage = 100;
        ArcaneEmpowermentSpell.ApplySpellDamage(caster, target, ref spellDamage);
        Assert.Equal(120, spellDamage);

        var healing = 100;
        ArcaneEmpowermentSpell.ApplyHealing(caster, ref healing);
        Assert.Equal(120, healing);

        Assert.Equal(70.0, ArcaneEmpowermentSpell.GetDispelDifficulty(follower));

        ArcaneEmpowermentSpell.ApplyEffectForTests(caster, 1200, 0);

        damage = 100;
        follower.AlterMeleeDamageTo(target, ref damage);
        Assert.Equal(110, damage);

        ArcaneEmpowermentSpell.StopEffect(caster);

        Assert.False(ArcaneEmpowermentSpell.IsUnderEffects(caster));
        Assert.Equal(100, follower.HitsMax);
        Assert.Equal(100, follower.Hits);
        Assert.Equal(50.0, ArcaneEmpowermentSpell.GetDispelDifficulty(follower));

        caster.Delete();
        target.Delete();
        follower.Delete();
    }

    [Fact]
    public void Logout_CleansUpTemporaryEffect()
    {
        var caster = NewCaster();
        ArcaneEmpowermentSpell.ApplyEffectForTests(caster, 1200, 1);

        EventSink.InvokeLogout(caster);

        Assert.False(ArcaneEmpowermentSpell.IsUnderEffects(caster));
        caster.Delete();
    }

    [Fact]
    public void DeathOrDeleteEvent_CleansUpTemporaryEffect()
    {
        var caster = NewCaster();
        ArcaneEmpowermentSpell.ApplyEffectForTests(caster, 1200, 1);

        ArcaneEmpowermentSpell.OnMobileRemoved(caster);

        Assert.False(ArcaneEmpowermentSpell.IsUnderEffects(caster));
        caster.Delete();
    }

    [Fact]
    public void PlayerVsPlayerDamage_DoesNotExceedTheSdiCap()
    {
        var caster = NewPlayerCaster();
        var target = NewPlayerCaster();
        var glasses = new WizardsGlasses();
        caster.AddItem(glasses);

        Assert.Equal(15, AosAttributes.GetValue(caster, AosAttribute.SpellDamage));

        ArcaneEmpowermentSpell.ApplyEffectForTests(caster, 1200, 5);

        var damage = 100;
        ArcaneEmpowermentSpell.ApplySpellDamage(caster, target, ref damage);

        Assert.Equal(100, damage);

        ArcaneEmpowermentSpell.StopEffect(caster);
        caster.Delete();
        target.Delete();
        glasses.Delete();
    }

    private static Mobile NewCaster(double spellweaving = 120.0)
    {
        var caster = new Mobile(World.NewMobile);
        caster.DefaultMobileInit();
        caster.InitStats(100, 100, 100);
        caster.Skills.Spellweaving.Base = spellweaving;
        return caster;
    }

    private static Mobile NewTarget()
    {
        var target = new Mobile(World.NewMobile);
        target.DefaultMobileInit();
        target.InitStats(200, 100, 100);
        target.Hits = 1;
        return target;
    }

    private static PlayerMobile NewPlayerCaster()
    {
        var caster = new PlayerMobile(World.NewMobile);
        caster.DefaultMobileInit();
        caster.Player = true;
        caster.InitStats(100, 100, 100);
        caster.Skills.Spellweaving.Base = 120.0;
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

    private sealed class TestSummon : BaseCreature
    {
        public TestSummon() : base(AIType.AI_Animal, FightMode.Closest, 10, 1)
        {
            Body = 0xC8;
        }

        public override double DispelDifficulty => 50.0;

        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.2;
            passiveSpeed = 0.4;
        }
    }
}
