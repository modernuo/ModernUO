using System;
using System.Collections.Generic;
using System.Reflection;
using Server.Items;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Mysticism;
using Xunit;

namespace Server.Tests.Spells.Mysticism;

[Collection("Sequential UOContent Tests")]
public class NetherBoltSpellTests
{
    private static Mobile NewMobile()
    {
        var m = new Mobile(World.NewMobile);
        m.DefaultMobileInit();
        m.InitStats(100, 100, 100);
        m.Mana = 100;
        return m;
    }

    [Fact]
    public void SpellMetadata_MatchesFirstCircleMysticismSources()
    {
        var caster = NewMobile();
        var spell = new NetherBoltSpell(caster);

        Assert.Equal("Nether Bolt", spell.Name);
        Assert.Equal("In Corp Ylem", spell.Mantra);
        Assert.Equal(SpellCircle.First, spell.Circle);
        Assert.Equal(TimeSpan.FromSeconds(0.5), spell.CastDelayBase);
        Assert.Equal(4, spell.GetMana());
        Assert.Equal(0.0, spell.RequiredSkill);
        Assert.Equal(SkillName.Mysticism, spell.CastSkill);
        Assert.Equal([Reagent.BlackPearl, Reagent.SulfurousAsh], spell.Reagents);
        Assert.True(spell.DelayedDamage);

        caster.Delete();
    }

    [Fact]
    public void MysticSpellbookAndScroll_UseNetherBoltSlot()
    {
        var book = new MysticSpellbook(1);
        var scroll = new NetherBoltScroll();

        Assert.Equal(677, book.BookOffset);
        Assert.Equal(16, book.BookCount);
        Assert.True(book.HasSpell(677));
        Assert.Equal(677, GetSpellScrollId(scroll));

        book.Delete();
        scroll.Delete();
    }

    [Fact]
    public void RegisterMysticism_PreSa_DoesNotExposeNetherBolt()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewMobile();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.ML;

            Initializer.Configure();

            Assert.Null(SpellRegistry.NewSpell(677, caster, null));
            Assert.Equal(-1, SpellRegistry.GetRegistryNumber(typeof(NetherBoltSpell)));
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
    public void RegisterMysticism_Sa_ExposesNetherBoltAtSpellId677()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewMobile();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.SA;

            Initializer.Configure();

            Assert.Same(typeof(NetherBoltSpell), SpellRegistry.Types[677]);
            Assert.Equal(677, SpellRegistry.GetRegistryNumber(typeof(NetherBoltSpell)));
            Assert.IsType<NetherBoltSpell>(SpellRegistry.NewSpell(677, caster, null));
            Assert.IsType<EagleStrikeSpell>(SpellRegistry.NewSpell(682, caster, null));
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
    public void DelayedDamageFamily_PreventsNetherBoltAndMagicArrowStackingInEitherOrder()
    {
        var caster = NewMobile();
        var target = NewMobile();
        var netherBolt = new NetherBoltSpell(caster);
        var magicArrow = new MagicArrowSpell(caster);

        netherBolt.StartDelayedDamageContext(target, new TestTimer());

        Assert.True(netherBolt.HasDelayedDamageContext(target));
        Assert.True(magicArrow.HasDelayedDamageContext(target));

        magicArrow.RemoveDelayedDamageContext(target);
        magicArrow.StartDelayedDamageContext(target, new TestTimer());

        Assert.True(magicArrow.HasDelayedDamageContext(target));
        Assert.True(netherBolt.HasDelayedDamageContext(target));

        netherBolt.RemoveDelayedDamageContext(target);
        caster.Delete();
        target.Delete();
    }

    [Fact]
    public void GetNewAosDamage_UsesHigherFocusOrImbuingSupportSkill()
    {
        var focusCaster = NewMobile();
        var imbuingCaster = NewMobile();
        var lowSupportCaster = NewMobile();
        var target = NewMobile();

        focusCaster.Skills.Mysticism.Base = 120.0;
        focusCaster.Skills.Focus.Base = 120.0;
        focusCaster.Skills.Imbuing.Base = 0.0;

        imbuingCaster.Skills.Mysticism.Base = 120.0;
        imbuingCaster.Skills.Focus.Base = 0.0;
        imbuingCaster.Skills.Imbuing.Base = 120.0;

        lowSupportCaster.Skills.Mysticism.Base = 120.0;
        lowSupportCaster.Skills.Focus.Base = 0.0;
        lowSupportCaster.Skills.Imbuing.Base = 0.0;

        using var random = new PredictableRandom(0);

        var focusDamage = new NetherBoltSpell(focusCaster).GetNewAosDamage(10, 1, 4, target);
        var imbuingDamage = new NetherBoltSpell(imbuingCaster).GetNewAosDamage(10, 1, 4, target);
        var lowSupportDamage = new NetherBoltSpell(lowSupportCaster).GetNewAosDamage(10, 1, 4, target);

        Assert.Equal(focusDamage, imbuingDamage);
        Assert.True(focusDamage > lowSupportDamage);

        focusCaster.Delete();
        imbuingCaster.Delete();
        lowSupportCaster.Delete();
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

    private sealed class TestTimer : Timer
    {
        public TestTimer() : base(TimeSpan.FromMinutes(5))
        {
        }

        protected override void OnTick()
        {
        }
    }
}
