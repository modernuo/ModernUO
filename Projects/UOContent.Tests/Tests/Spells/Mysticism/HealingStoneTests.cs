using System;
using System.Collections.Generic;
using System.Reflection;
using Server.Items;
using Server.Spells;
using Server.Spells.Mysticism;
using Xunit;

namespace Server.Tests.Spells.Mysticism;

[Collection("Sequential UOContent Tests")]
public class HealingStoneTests
{
    [Fact]
    public void SpellMetadata_MatchesHealingStoneSources()
    {
        var caster = NewMysticCaster();
        var spell = new HealingStoneSpell(caster);

        Assert.Equal("Healing Stone", spell.Name);
        Assert.Equal("Kal In Mani", spell.Mantra);
        Assert.Equal(SpellCircle.First, spell.Circle);
        Assert.Equal(TimeSpan.FromSeconds(5.0), spell.CastDelayBase);
        Assert.Equal(4, spell.GetMana());
        Assert.Equal(0.0, spell.RequiredSkill);
        Assert.Equal(SkillName.Mysticism, spell.CastSkill);
        Assert.Equal([Reagent.Bone, Reagent.Garlic, Reagent.Ginseng, Reagent.SpidersSilk], spell.Reagents);

        caster.Delete();
    }

    [Fact]
    public void MysticSpellbookAndScroll_UseHealingStoneSlot()
    {
        var book = new MysticSpellbook(1UL << (678 - 677));
        var scroll = new HealingStoneScroll();

        Assert.Equal(677, book.BookOffset);
        Assert.Equal(16, book.BookCount);
        Assert.True(book.HasSpell(678));
        Assert.Equal(678, GetSpellScrollId(scroll));

        book.Delete();
        scroll.Delete();
    }

    [Fact]
    public void RegisterMysticism_PreSa_DoesNotExposeHealingStone()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewMysticCaster();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.ML;
            Initializer.Configure();

            Assert.Null(SpellRegistry.NewSpell(678, caster, null));
            Assert.Equal(-1, SpellRegistry.GetRegistryNumber(typeof(HealingStoneSpell)));
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
    public void RegisterMysticism_Sa_ExposesHealingStoneAtSpellId678()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewMysticCaster();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.SA;
            Initializer.Configure();

            Assert.Same(typeof(HealingStoneSpell), SpellRegistry.Types[678]);
            Assert.Equal(678, SpellRegistry.GetRegistryNumber(typeof(HealingStoneSpell)));
            Assert.IsType<HealingStoneSpell>(SpellRegistry.NewSpell(678, caster, null));
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
    public void CastingHealingStone_ConsumesResourcesAndCreatesOneOwnedStone()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewMysticCaster();
        AddReagents(caster);

        try
        {
            Core.Expansion = Expansion.SA;
            var spell = new TestHealingStoneSpell(caster);
            caster.Spell = spell;
            spell.State = SpellState.Sequencing;

            spell.OnCast();

            var stone = caster.Backpack.FindItemByType<HealingStone>();
            Assert.NotNull(stone);
            Assert.Same(caster, stone.Owner);
            Assert.Equal(96, caster.Mana);
            Assert.Equal(9, caster.Backpack.FindItemByType<Bone>().Amount);
            Assert.Equal(9, caster.Backpack.FindItemByType<Garlic>().Amount);
            Assert.Equal(9, caster.Backpack.FindItemByType<Ginseng>().Amount);
            Assert.Equal(9, caster.Backpack.FindItemByType<SpidersSilk>().Amount);
        }
        finally
        {
            Core.Expansion = previousExpansion;
            caster.Delete();
        }
    }

    [Fact]
    public void HealingStone_HealsOnlyOwnerAndRequiresBackpack()
    {
        var owner = NewMobile();
        var other = NewMobile();
        var stone = new HealingStone(owner, 100, 20);
        owner.AddToBackpack(stone);
        owner.Hits = owner.HitsMax - 20;

        try
        {
            Assert.False(stone.TryUseForTests(other));
            Assert.Equal(100, stone.LifeForce);

            owner.Hits = owner.HitsMax;
            Assert.False(stone.TryUseForTests(owner));
            Assert.Equal(100, stone.LifeForce);

            owner.Hits = owner.HitsMax - 20;
            Assert.True(stone.TryUseForTests(owner));
            Assert.Equal(owner.HitsMax, owner.Hits);
            Assert.Equal(80, stone.LifeForce);
            Assert.Equal(1, stone.AvailableHealing);

            owner.EndAction<HealingStone>();
            stone.MoveToWorld(owner.Location, owner.Map);
            Assert.False(stone.TryUseForTests(owner));
        }
        finally
        {
            owner.Delete();
            other.Delete();
            stone.Delete();
        }
    }

    [Fact]
    public void HealingStone_RechargesOneUseOverFifteenSeconds()
    {
        var previousNow = Core.Now;
        var owner = NewMobile();
        var stone = new HealingStone(owner, 100, 30);
        owner.AddToBackpack(stone);
        owner.Hits = owner.HitsMax - 30;

        try
        {
            Assert.True(stone.TryUseForTests(owner));
            owner.EndAction<HealingStone>();
            Assert.Equal(1, stone.AvailableHealing);

            Core._now = previousNow.AddSeconds(HealingStone.FullRechargeSeconds);
            Assert.Equal(stone.MaxHealing, stone.AvailableHealing);
        }
        finally
        {
            Core._now = previousNow;
            owner.Delete();
            stone.Delete();
        }
    }

    [Fact]
    public void HealingStone_RejectsSecureTradeAndDeletesWhenDropped()
    {
        var owner = NewMobile();
        var other = NewMobile();
        var stone = new HealingStone(owner, 100, 20);
        owner.AddToBackpack(stone);

        Assert.False(stone.AllowSecureTrade(owner, other, owner, true));
        Assert.False(stone.DropToWorld(owner, Point3D.Zero));
        Assert.True(stone.Deleted);

        owner.Delete();
        other.Delete();
    }

    [Fact]
    public void HealingStone_CureCostAndChanceScaleByPoisonLevel()
    {
        Assert.Equal(0, HealingStone.GetCureCost(0));
        Assert.Equal(25, HealingStone.GetCureCost(1));
        Assert.Equal(100, HealingStone.GetCureCost(4));
        Assert.Equal(120, HealingStone.GetCureCost(5));
        Assert.Equal(170.0, HealingStone.GetCureChance(120.0, 120.0, 0));
        Assert.Equal(90.0, HealingStone.GetCureChance(120.0, 120.0, 4));
    }

    [Fact]
    public void HealingPotion_ResetsStonePerUseHealing()
    {
        var previousNow = Core.Now;
        var owner = NewMobile();
        var stone = new HealingStone(owner, 100, 20);
        owner.AddToBackpack(stone);
        owner.Hits = owner.HitsMax - 20;

        Assert.True(stone.TryUseForTests(owner));
        owner.EndAction<HealingStone>();
        Assert.Equal(1, stone.AvailableHealing);

        Core._now = previousNow.AddSeconds(HealingStone.FullRechargeSeconds);
        Assert.Equal(stone.MaxHealing, stone.AvailableHealing);

        var potion = new TestHealPotion();
        owner.Hits = owner.HitsMax - 10;
        potion.DoHeal(owner);

        Assert.Equal(1, stone.AvailableHealing);

        potion.Delete();
        owner.Delete();
        stone.Delete();
        Core._now = previousNow;
    }

    private static Mobile NewMobile(bool withBackpack = true)
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.Player = true;
        mobile.InitStats(100, 100, 100);
        mobile.Hits = mobile.HitsMax;
        mobile.Mana = mobile.ManaMax;

        if (withBackpack)
        {
            mobile.AddItem(new Backpack());
        }

        return mobile;
    }

    private static Mobile NewMysticCaster()
    {
        var caster = NewMobile();
        caster.Skills.Mysticism.Base = 120.0;
        caster.Skills.Focus.Base = 120.0;
        caster.Skills.Imbuing.Base = 0.0;
        return caster;
    }

    private static void AddReagents(Mobile caster)
    {
        caster.AddToBackpack(new Bone(10));
        caster.AddToBackpack(new Garlic(10));
        caster.AddToBackpack(new Ginseng(10));
        caster.AddToBackpack(new SpidersSilk(10));
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

    private sealed class TestHealingStoneSpell : HealingStoneSpell
    {
        public TestHealingStoneSpell(Mobile caster) : base(caster)
        {
        }

        public override bool CheckFizzle() => true;
    }

    private sealed class TestHealPotion : BaseHealPotion
    {
        public TestHealPotion() : base(PotionEffect.Heal)
        {
        }

        public override int MinHeal => 10;
        public override int MaxHeal => 10;
        public override double Delay => 10.0;
    }
}
