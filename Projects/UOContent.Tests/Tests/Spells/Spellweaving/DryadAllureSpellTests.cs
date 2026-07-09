using System;
using System.Collections.Generic;
using System.Reflection;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Spellweaving;
using Xunit;

namespace Server.Tests.Spells.Spellweaving;

[Collection("Sequential UOContent Tests")]
public class DryadAllureSpellTests
{
    private static Mobile NewCaster(double spellweaving = 120.0)
    {
        var caster = new Mobile(World.NewMobile);
        caster.DefaultMobileInit();
        caster.InitStats(100, 100, 100);
        caster.Mana = 100;
        caster.Skills.Spellweaving.Base = spellweaving;
        return caster;
    }

    [Fact]
    public void SpellMetadata_MatchesDryadAllureSources()
    {
        var caster = NewCaster();
        var spell = new DryadAllureSpell(caster);

        Assert.Equal("Dryad Allure", spell.Name);
        Assert.Equal("Rathril", spell.Mantra);
        Assert.Equal(TimeSpan.FromSeconds(3.0), spell.CastDelayBase);
        Assert.Equal(52.0, spell.RequiredSkill);
        Assert.Equal(40, spell.GetMana());
        Assert.Equal(SkillName.Spellweaving, spell.CastSkill);

        caster.Delete();
    }

    [Fact]
    public void SpellweavingBookAndScroll_UseDryadAllureSlot()
    {
        var book = new SpellweavingBook(1UL << (611 - 600));
        var scroll = new DryadAllureScroll();

        Assert.Equal(600, book.BookOffset);
        Assert.Equal(16, book.BookCount);
        Assert.True(book.HasSpell(611));
        Assert.Equal(611, GetSpellScrollId(scroll));

        book.Delete();
        scroll.Delete();
    }

    [Fact]
    public void RegisterSpellweaving_PreMl_DoesNotExposeDryadAllure()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.SE;

            Initializer.Configure();

            Assert.Null(SpellRegistry.NewSpell(611, caster, null));
            Assert.Equal(-1, SpellRegistry.GetRegistryNumber(typeof(DryadAllureSpell)));
            Assert.IsType<MagicArrowSpell>(SpellRegistry.NewSpell(4, caster, null));
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
    public void RegisterSpellweaving_Ml_ExposesDryadAllureAtSpellId611()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.ML;

            Initializer.Configure();

            Assert.Same(typeof(DryadAllureSpell), SpellRegistry.Types[611]);
            Assert.Equal(611, SpellRegistry.GetRegistryNumber(typeof(DryadAllureSpell)));
            Assert.IsType<DryadAllureSpell>(SpellRegistry.NewSpell(611, caster, null));
            Assert.IsType<EssenceOfWindSpell>(SpellRegistry.NewSpell(610, caster, null));
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
    public void IsValidTarget_AllowsRepondHumanoidsAndRejectsUnsafeTargets()
    {
        var validOrc = new TestOrc();
        var controlledOrc = new TestOrc();
        var summonedOrc = new TestOrc { Summoned = true };
        var paragonOrc = new TestOrc { IsParagon = true };
        var immuneOrc = new AllureImmuneOrc();
        var nonRepond = new TestCreature();
        var master = NewCaster();

        controlledOrc.SetControlMaster(master);

        Assert.True(DryadAllureSpell.IsValidTarget(validOrc));
        Assert.False(DryadAllureSpell.IsValidTarget(controlledOrc));
        Assert.False(DryadAllureSpell.IsValidTarget(summonedOrc));
        Assert.False(DryadAllureSpell.IsValidTarget(paragonOrc));
        Assert.False(DryadAllureSpell.IsValidTarget(immuneOrc));
        Assert.False(DryadAllureSpell.IsValidTarget(nonRepond));

        master.Delete();
        validOrc.Delete();
        controlledOrc.Delete();
        summonedOrc.Delete();
        paragonOrc.Delete();
        immuneOrc.Delete();
        nonRepond.Delete();
    }

    [Fact]
    public void GetCharmChance_IncludesSpellweavingSkillAndFocusBonus()
    {
        var noFocusChance = DryadAllureSpell.GetCharmChance(75.0, 0);
        var focusChance = DryadAllureSpell.GetCharmChance(75.0, 3);

        Assert.Equal(0.5, noFocusChance, 3);
        Assert.Equal(0.56, focusChance, 3);
    }

    [Fact]
    public void ApplyAllureAttempt_SuccessControlsTargetAsThreeSlotAlluredFollowerAndDeletesBackpackContents()
    {
        var caster = NewCaster();
        var target = new TestOrc();
        target.PackItem(new Gold(10));
        caster.Combatant = target;
        caster.Warmode = true;
        target.Combatant = caster;
        target.Warmode = true;

        using var random = new PredictableRandom(0);

        Assert.True(DryadAllureSpell.ApplyAllureAttempt(caster, target, focusLevel: 0));
        Assert.True(target.Controlled);
        Assert.Equal(caster, target.ControlMaster);
        Assert.True(target.Allured);
        Assert.Equal(3, target.ControlSlots);
        Assert.Equal(3, caster.Followers);
        Assert.Equal(BaseCreature.MaxLoyalty, target.Loyalty);
        Assert.Null(caster.Combatant);
        Assert.Null(target.Combatant);
        Assert.False(caster.Warmode);
        Assert.False(target.Warmode);
        Assert.Empty(target.Backpack?.Items ?? []);

        caster.Delete();
        target.Delete();
    }

    [Fact]
    public void ApplyAllureAttempt_FailureEnragesTargetWithoutFollowerLeak()
    {
        var caster = NewCaster(spellweaving: 52.0);
        var target = new TestOrc();

        using var random = new PredictableRandom(20);

        Assert.False(DryadAllureSpell.ApplyAllureAttempt(caster, target, focusLevel: 0));
        Assert.False(target.Controlled);
        Assert.False(target.Allured);
        Assert.Equal(caster, target.ControlTarget);
        Assert.Equal(OrderType.Attack, target.ControlOrder);
        Assert.Equal(caster, target.Combatant);
        Assert.True(target.Warmode);
        Assert.Equal(0, caster.Followers);

        caster.Delete();
        target.Delete();
    }

    [Fact]
    public void SetControlMasterNull_ClearsAlluredStateOnRelease()
    {
        var caster = NewCaster();
        var target = new TestOrc();

        target.ControlSlots = 3;
        target.SetControlMaster(caster);
        target.Allured = true;

        target.SetControlMaster(null);

        Assert.False(target.Controlled);
        Assert.Null(target.ControlMaster);
        Assert.False(target.Allured);
        Assert.Equal(0, caster.Followers);

        caster.Delete();
        target.Delete();
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

    private class TestOrc : Orc
    {
        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.2;
            passiveSpeed = 0.4;
        }
    }

    private class AllureImmuneOrc : TestOrc
    {
        public override bool AllureImmune => true;
    }

    private class TestCreature : BaseCreature
    {
        public TestCreature() : base(AIType.AI_Animal, FightMode.Closest, 10, 1)
        {
            Body = 0xC8;
        }

        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.2;
            passiveSpeed = 0.4;
        }
    }
}
