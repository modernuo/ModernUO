using System;
using System.Collections.Generic;
using System.Reflection;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.First;
using Server.Spells.Mysticism;
using Server.Spells.Second;
using Server.Spells.Third;
using Xunit;

namespace Server.Tests.Spells.Mysticism;

[Collection("Sequential UOContent Tests")]
public class PurgeMagicSpellTests
{
    private static Mobile NewMobile()
    {
        var m = new Mobile(World.NewMobile);
        m.DefaultMobileInit();
        m.InitStats(100, 100, 100);
        m.Hits = 100;
        m.Mana = 100;
        return m;
    }

    private static Mobile NewMysticCaster()
    {
        var caster = NewMobile();
        caster.Skills.Mysticism.Base = 120.0;
        caster.Skills.Focus.Base = 120.0;
        caster.Skills.Imbuing.Base = 0.0;
        return caster;
    }

    [Fact]
    public void SpellMetadata_MatchesSecondCircleMysticismSources()
    {
        var caster = NewMysticCaster();
        var spell = new PurgeMagicSpell(caster);

        Assert.Equal("Purge Magic", spell.Name);
        Assert.Equal("An Ort Sanct", spell.Mantra);
        Assert.Equal(SpellCircle.Second, spell.Circle);
        Assert.Equal(TimeSpan.FromSeconds(0.75), spell.CastDelayBase);
        Assert.Equal(6, spell.GetMana());
        Assert.Equal(8.0, spell.RequiredSkill);
        Assert.Equal(SkillName.Mysticism, spell.CastSkill);
        Assert.Equal(
            [Reagent.FertileDirt, Reagent.Garlic, Reagent.MandrakeRoot, Reagent.SulfurousAsh],
            spell.Reagents
        );

        caster.Delete();
    }

    [Fact]
    public void MysticSpellbookAndScroll_UsePurgeMagicSlot()
    {
        var book = new MysticSpellbook(1UL << (679 - 677));
        var scroll = new PurgeMagicScroll();

        Assert.Equal(677, book.BookOffset);
        Assert.Equal(16, book.BookCount);
        Assert.True(book.HasSpell(679));
        Assert.Equal(679, GetSpellScrollId(scroll));

        book.Delete();
        scroll.Delete();
    }

    [Fact]
    public void RegisterMysticism_PreSa_DoesNotExposePurgeMagic()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewMysticCaster();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.ML;

            Initializer.Configure();

            Assert.Null(SpellRegistry.NewSpell(679, caster, null));
            Assert.Equal(-1, SpellRegistry.GetRegistryNumber(typeof(PurgeMagicSpell)));
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
    public void RegisterMysticism_Sa_ExposesPurgeMagicAtSpellId679()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewMysticCaster();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.SA;

            Initializer.Configure();

            Assert.Same(typeof(PurgeMagicSpell), SpellRegistry.Types[679]);
            Assert.Equal(679, SpellRegistry.GetRegistryNumber(typeof(PurgeMagicSpell)));
            Assert.IsType<PurgeMagicSpell>(SpellRegistry.NewSpell(679, caster, null));
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

    [Theory]
    [InlineData(PurgeMagicSpell.PurgeWardType.MagicReflection)]
    [InlineData(PurgeMagicSpell.PurgeWardType.Protection)]
    [InlineData(PurgeMagicSpell.PurgeWardType.ReactiveArmor)]
    [InlineData(PurgeMagicSpell.PurgeWardType.Bless)]
    public void ApplyPurge_RemovesSupportedWard(PurgeMagicSpell.PurgeWardType wardType)
    {
        var caster = NewMysticCaster();
        var target = NewMobile();
        target.Skills.MagicResist.Base = 0.0;

        try
        {
            ApplyWard(wardType, target);
            Assert.True(HasWard(wardType, target));

            using var random = new PredictableRandom(20);

            Assert.True(PurgeMagicSpell.ApplyPurge(caster, target));
            Assert.False(HasWard(wardType, target));
        }
        finally
        {
            PurgeMagicSpell.ClearState(target);
            caster.Delete();
            target.Delete();
        }
    }

    [Fact]
    public void ApplyPurge_RemovesExactlyOneSupportedWard()
    {
        var caster = NewMysticCaster();
        var target = NewMobile();
        target.Skills.MagicResist.Base = 0.0;

        try
        {
            ApplyWard(PurgeMagicSpell.PurgeWardType.Protection, target);
            ApplyWard(PurgeMagicSpell.PurgeWardType.ReactiveArmor, target);

            using var random = new PredictableRandom(20);

            Assert.True(PurgeMagicSpell.ApplyPurge(caster, target));

            var remaining = 0;
            remaining += ProtectionSpell.HasEffect(target) ? 1 : 0;
            remaining += ReactiveArmorSpell.HasAosEffect(target) ? 1 : 0;
            Assert.Equal(1, remaining);
        }
        finally
        {
            PurgeMagicSpell.ClearState(target);
            ProtectionSpell.EndProtection(target);
            ReactiveArmorSpell.EndArmor(target);
            caster.Delete();
            target.Delete();
        }
    }

    [Fact]
    public void ApplyPurge_ReappliedWardBypassesStandardImmunityForThatWard()
    {
        var caster = NewMysticCaster();
        var target = NewMobile();
        target.Skills.MagicResist.Base = 0.0;

        try
        {
            ApplyWard(PurgeMagicSpell.PurgeWardType.Protection, target);

            using (new PredictableRandom(20))
            {
                Assert.True(PurgeMagicSpell.ApplyPurge(caster, target));
            }

            Assert.False(ProtectionSpell.HasEffect(target));
            Assert.True(PurgeMagicSpell.IsImmuneToPurge(target, null));

            ApplyWard(PurgeMagicSpell.PurgeWardType.Protection, target);

            using (new PredictableRandom(20))
            {
                Assert.True(PurgeMagicSpell.ApplyPurge(caster, target));
            }

            Assert.False(ProtectionSpell.HasEffect(target));
        }
        finally
        {
            PurgeMagicSpell.ClearState(target);
            ProtectionSpell.EndProtection(target);
            caster.Delete();
            target.Delete();
        }
    }

    [Fact]
    public void ApplyPurge_StandardImmunityWithoutReappliedWardBlocksDisruption()
    {
        var caster = NewMysticCaster();
        var target = NewMobile();
        target.Skills.MagicResist.Base = 0.0;

        try
        {
            ApplyWard(PurgeMagicSpell.PurgeWardType.Protection, target);

            using (new PredictableRandom(20))
            {
                Assert.True(PurgeMagicSpell.ApplyPurge(caster, target));
            }

            Assert.True(PurgeMagicSpell.IsImmuneToPurge(target, null));
            Assert.False(PurgeMagicSpell.ApplyPurge(caster, target));
            Assert.False(PurgeMagicSpell.IsManaDisrupted(target));
        }
        finally
        {
            PurgeMagicSpell.ClearState(target);
            caster.Delete();
            target.Delete();
        }
    }

    [Fact]
    public void ApplyPurge_WithoutWard_AppliesManaDisruptionAndOutgoingDamageClearsIt()
    {
        var caster = NewMysticCaster();
        var target = NewMobile();
        var defender = NewMobile();

        try
        {
            Assert.True(PurgeMagicSpell.ApplyPurge(caster, target));
            Assert.True(PurgeMagicSpell.IsManaDisrupted(target));
            Assert.True(new NetherBoltSpell(target).ScaleMana(10) > 10);

            var hits = target.Hits;
            PurgeMagicSpell.OnMobileDamaged(target, defender, 1);

            Assert.False(PurgeMagicSpell.IsManaDisrupted(target));
            Assert.True(target.Hits < hits);
        }
        finally
        {
            PurgeMagicSpell.ClearState(target);
            caster.Delete();
            target.Delete();
            defender.Delete();
        }
    }

    [Fact]
    public void CheckCast_FailsWhileCasterManaIsDisrupted()
    {
        var caster = NewMysticCaster();
        var attacker = NewMysticCaster();

        try
        {
            Assert.True(PurgeMagicSpell.ApplyManaDisruption(attacker, caster));

            var spell = new PurgeMagicSpell(caster);

            Assert.False(spell.CheckCast());
        }
        finally
        {
            PurgeMagicSpell.ClearState(caster);
            caster.Delete();
            attacker.Delete();
        }
    }

    [Fact]
    public void ClearState_RemovesManaDisruptionAndImmunityWithoutDamage()
    {
        var caster = NewMysticCaster();
        var target = NewMobile();

        try
        {
            Assert.True(PurgeMagicSpell.ApplyManaDisruption(caster, target));
            var hits = target.Hits;

            PurgeMagicSpell.ClearState(target);

            Assert.False(PurgeMagicSpell.IsManaDisrupted(target));
            Assert.Equal(hits, target.Hits);
        }
        finally
        {
            caster.Delete();
            target.Delete();
        }
    }

    private static void ApplyWard(PurgeMagicSpell.PurgeWardType wardType, Mobile target)
    {
        switch (wardType)
        {
            case PurgeMagicSpell.PurgeWardType.MagicReflection:
                ApplyMagicReflection(target);
                break;
            case PurgeMagicSpell.PurgeWardType.Protection:
                ProtectionSpell.Toggle(target, target);
                break;
            case PurgeMagicSpell.PurgeWardType.ReactiveArmor:
                new ReactiveArmorSpell(target).OnCastForTests();
                break;
            case PurgeMagicSpell.PurgeWardType.Bless:
                var duration = TimeSpan.FromMinutes(1);
                SpellHelper.AddStatBonus(target, target, StatType.Str, 10, duration);
                SpellHelper.AddStatBonus(target, target, StatType.Dex, 10, duration);
                SpellHelper.AddStatBonus(target, target, StatType.Int, 10, duration);
                (target as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.Bless, 1075847, 1075848, duration, "10\t10\t10"));
                break;
        }
    }

    private static bool HasWard(PurgeMagicSpell.PurgeWardType wardType, Mobile target) => wardType switch
    {
        PurgeMagicSpell.PurgeWardType.MagicReflection => MagicReflectSpell.HasEffect(target),
        PurgeMagicSpell.PurgeWardType.Protection      => ProtectionSpell.HasEffect(target),
        PurgeMagicSpell.PurgeWardType.ReactiveArmor   => ReactiveArmorSpell.HasAosEffect(target),
        PurgeMagicSpell.PurgeWardType.Bless           => target.GetStatMod("[Magic] Str Buff") != null &&
                                                         target.GetStatMod("[Magic] Dex Buff") != null &&
                                                         target.GetStatMod("[Magic] Int Buff") != null,
        _                                             => false
    };

    private static void ApplyMagicReflection(Mobile target)
    {
        var mods = new[]
        {
            new ResistanceMod(ResistanceType.Physical, "PhysicalResistMagicResist", -20),
            new ResistanceMod(ResistanceType.Fire, "FireResistMagicResist", 10),
            new ResistanceMod(ResistanceType.Cold, "ColdResistMagicResist", 10),
            new ResistanceMod(ResistanceType.Poison, "PoisonResistMagicResist", 10),
            new ResistanceMod(ResistanceType.Energy, "EnergyResistMagicResist", 10)
        };

        foreach (var mod in mods)
        {
            target.AddResistanceMod(mod);
        }

        var table = (Dictionary<Mobile, ResistanceMod[]>)typeof(MagicReflectSpell)
            .GetField("_table", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
        table[target] = mods;
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

internal static class ReactiveArmorSpellTestExtensions
{
    public static void OnCastForTests(this ReactiveArmorSpell spell)
    {
        var caster = spell.Caster;
        var mods = new[]
        {
            new ResistanceMod(ResistanceType.Physical, "PhysicalResistReactiveArmorSpell", 20),
            new ResistanceMod(ResistanceType.Fire, "FireResistReactiveArmorSpell", -5),
            new ResistanceMod(ResistanceType.Cold, "ColdResistReactiveArmorSpell", -5),
            new ResistanceMod(ResistanceType.Poison, "PoisonResistReactiveArmorSpell", -5),
            new ResistanceMod(ResistanceType.Energy, "EnergyResistReactiveArmorSpell", -5)
        };

        foreach (var mod in mods)
        {
            caster.AddResistanceMod(mod);
        }

        var tableField = typeof(ReactiveArmorSpell).GetField("_table", BindingFlags.Static | BindingFlags.NonPublic)!;
        var table = (Dictionary<Mobile, ResistanceMod[]>?)tableField.GetValue(null);
        table ??= [];
        table[caster] = mods;
        tableField.SetValue(null, table);
    }
}
