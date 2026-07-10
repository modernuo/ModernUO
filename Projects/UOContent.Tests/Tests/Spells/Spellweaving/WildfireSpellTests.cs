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
public class WildfireSpellTests
{
    [Fact]
    public void SpellMetadata_MatchesWildfireSources()
    {
        var caster = NewCaster();
        var spell = new WildfireSpell(caster);

        Assert.Equal("Wildfire", spell.Name);
        Assert.Equal("Haelyn", spell.Mantra);
        Assert.Equal(TimeSpan.FromSeconds(2.5), spell.CastDelayBase);
        Assert.Equal(66.0, spell.RequiredSkill);
        Assert.Equal(50, spell.GetMana());
        Assert.Equal(SkillName.Spellweaving, spell.CastSkill);
        Assert.Equal(SkillName.Spellweaving, spell.DamageSkill);
        Assert.False(spell.ClearHandsOnCast);

        caster.Delete();
    }

    [Fact]
    public void SpellweavingBookAndScroll_UseWildfireSlot()
    {
        var book = new SpellweavingBook(1UL << (609 - 600));
        var scroll = new WildfireScroll();

        Assert.Equal(600, book.BookOffset);
        Assert.Equal(16, book.BookCount);
        Assert.True(book.HasSpell(609));
        Assert.Equal(609, GetSpellScrollId(scroll));

        book.Delete();
        scroll.Delete();
    }

    [Fact]
    public void RegisterSpellweaving_PreMlDoesNotExposeWildfire()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.SE;
            Initializer.Configure();

            Assert.Null(SpellRegistry.NewSpell(609, caster, null));
            Assert.Equal(-1, SpellRegistry.GetRegistryNumber(typeof(WildfireSpell)));
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
    public void RegisterSpellweaving_MlExposesWildfireAtSpellId609()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.ML;
            Initializer.Configure();

            Assert.Same(typeof(WildfireSpell), SpellRegistry.Types[609]);
            Assert.Equal(609, SpellRegistry.GetRegistryNumber(typeof(WildfireSpell)));
            Assert.IsType<WildfireSpell>(SpellRegistry.NewSpell(609, caster, null));
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
    public void CheckCast_UsesSkillAndManaRequirements()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster(65.9);

        try
        {
            Core.Expansion = Expansion.ML;
            var spell = new WildfireSpell(caster);

            Assert.False(spell.CheckCast());

            caster.Skills.Spellweaving.Base = 66.0;
            caster.Mana = 0;
            Assert.False(spell.CheckCast());

            caster.Mana = caster.ManaMax;
            Assert.True(spell.CheckCast());
        }
        finally
        {
            Core.Expansion = previousExpansion;
            caster.Delete();
        }
    }

    [Theory]
    [InlineData(0.0, 0, 10, 1, 5)]
    [InlineData(66.0, 0, 12, 2, 5)]
    [InlineData(120.0, 0, 15, 5, 5)]
    [InlineData(66.0, 3, 15, 5, 8)]
    public void FocusAndSkillScaleBaseDamageDurationAndRadius(
        double skill,
        int focus,
        int expectedDamage,
        int expectedDurationSeconds,
        int expectedRadius
    )
    {
        Assert.Equal(expectedDamage, WildfireSpell.GetBaseDamage(skill, focus));
        Assert.Equal(TimeSpan.FromSeconds(expectedDurationSeconds), WildfireSpell.GetDuration(skill, focus));
        Assert.Equal(expectedRadius, WildfireSpell.GetRadius(focus));
    }

    [Theory]
    [InlineData(15, 1, 15)]
    [InlineData(15, 2, 7)]
    [InlineData(15, 3, 5)]
    [InlineData(11, 3, 5)]
    public void DamageSplitsAcrossMultipleTargets(int baseDamage, int targetCount, int expectedDamage)
    {
        Assert.Equal(expectedDamage, WildfireSpell.GetDamageForTargetCount(baseDamage, targetCount));
    }

    [Fact]
    public void SpellDamageIncreaseIsAppliedAfterSplitAndCappedInPlayerCombat()
    {
        Assert.Equal(11, WildfireSpell.GetDamageAfterSdi(10, 100, true));
        Assert.Equal(20, WildfireSpell.GetDamageAfterSdi(10, 100, false));
        Assert.Equal(8, WildfireSpell.GetDamageAfterSdi(7, 15, true));
    }

    [Fact]
    public void TickDamagesVisibleHostileTargetsButNotHiddenTargets()
    {
        var caster = NewCaster();
        var target = new TestOrc();
        var location = new Point3D(100, 100, 0);
        var spell = new WildfireSpell(caster);

        try
        {
            caster.MoveToWorld(location, Map.Felucca);
            target.MoveToWorld(new Point3D(101, 100, 0), Map.Felucca);
            target.Hits = target.HitsMax;

            WildfireSpell.ClearAllForTests();
            spell.StartTimerForTests(location, Map.Felucca, 5, 10, TimeSpan.FromSeconds(1));
            var visibleHits = target.Hits;
            spell.TickForTests();

            Assert.True(target.Hits < visibleHits);

            WildfireSpell.ClearAllForTests();
            target.Hits = visibleHits;
            target.Hidden = true;
            spell.TickForTests();

            Assert.Equal(visibleHits, target.Hits);
        }
        finally
        {
            WildfireSpell.ClearAllForTests();
            DeleteFireItems(target.Location);
            target.Delete();
            caster.Delete();
        }
    }

    [Fact]
    public void FireItemsStopTheirTimerOnDeletionAndDoNotSurviveDeserialization()
    {
        var item = new WildfireFireItem(new Point3D(100, 100, 0), Map.Felucca, TimeSpan.FromMinutes(1));
        var timerField = typeof(WildfireFireItem).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic);
        var timer = timerField!.GetValue(item);

        item.Delete();

        Assert.Null(timerField.GetValue(item));
        Assert.NotNull(timer);

        var deserializedItem = new WildfireFireItem(new Point3D(100, 100, 0), Map.Felucca, TimeSpan.FromMinutes(1));
        var afterDeserialization = typeof(WildfireFireItem).GetMethod(
            "AfterDeserialization",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        afterDeserialization!.Invoke(deserializedItem, null);

        Assert.True(deserializedItem.Deleted);
        DeleteFireItems(new Point3D(100, 100, 0));
    }

    private static Mobile NewCaster(double spellweaving = 120.0)
    {
        var caster = new Mobile(World.NewMobile);
        caster.DefaultMobileInit();
        caster.InitStats(100, 100, 100);
        caster.Mana = caster.ManaMax;
        caster.Skills.Spellweaving.Base = spellweaving;
        return caster;
    }

    private static void DeleteFireItems(Point3D location)
    {
        var items = new List<WildfireFireItem>();

        foreach (var item in Map.Felucca.GetItemsInRange(location, 2))
        {
            if (item is WildfireFireItem)
            {
                items.Add((WildfireFireItem)item);
            }
        }

        foreach (var item in items)
        {
            item.Delete();
        }
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

    private sealed class TestOrc : Orc
    {
        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.2;
            passiveSpeed = 0.4;
        }
    }
}
