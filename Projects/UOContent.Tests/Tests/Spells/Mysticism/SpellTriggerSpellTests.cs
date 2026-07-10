using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Spells.Mysticism;
using Xunit;

namespace Server.Tests.Spells.Mysticism;

[Collection("Sequential UOContent Tests")]
public class SpellTriggerSpellTests
{
    [Fact]
    public void SpellMetadata_MatchesFifthCircleMysticismSources()
    {
        var caster = NewCaster();
        var spell = new SpellTriggerSpell(caster);

        Assert.Equal("Spell Trigger", spell.Name);
        Assert.Equal("In Vas Ort Ex", spell.Mantra);
        Assert.Equal(SpellCircle.Fifth, spell.Circle);
        Assert.Equal(TimeSpan.FromSeconds(1.5), spell.CastDelayBase);
        Assert.Equal(14, spell.GetMana());
        Assert.Equal(45.0, spell.RequiredSkill);
        Assert.Equal(SkillName.Mysticism, spell.CastSkill);
        Assert.Equal(
            [Reagent.DragonsBlood, Reagent.Garlic, Reagent.MandrakeRoot, Reagent.SpidersSilk],
            spell.Reagents
        );

        caster.Delete();
    }

    [Fact]
    public void MysticSpellbookAndScroll_UseSpellTriggerSlot()
    {
        var book = new MysticSpellbook(1UL << (685 - 677));
        var scroll = new SpellTriggerScroll();

        Assert.Equal(677, book.BookOffset);
        Assert.Equal(16, book.BookCount);
        Assert.True(book.HasSpell(685));
        Assert.Equal(685, GetSpellScrollId(scroll));

        book.Delete();
        scroll.Delete();
    }

    [Fact]
    public void RegisterMysticism_PreSaDoesNotExposeSpellTrigger()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.ML;
            Initializer.Configure();

            Assert.Null(SpellRegistry.NewSpell(685, caster, null));
            Assert.Equal(-1, SpellRegistry.GetRegistryNumber(typeof(SpellTriggerSpell)));
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
    public void RegisterMysticism_SaExposesSpellTriggerAtSpellId685()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.SA;
            Initializer.Configure();

            Assert.Same(typeof(SpellTriggerSpell), SpellRegistry.Types[685]);
            Assert.Equal(685, SpellRegistry.GetRegistryNumber(typeof(SpellTriggerSpell)));
            Assert.IsType<SpellTriggerSpell>(SpellRegistry.NewSpell(685, caster, null));
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
    public void MaximumStorableCircle_UsesCombinedMysticismAndSupportSkillAndCapsAtSix()
    {
        var caster = NewCaster(20.0, 20.0);
        var cappedCaster = NewCaster();

        Assert.Equal(1, SpellTriggerSpell.GetMaximumStorableCircle(caster));
        Assert.Equal(6, SpellTriggerSpell.GetMaximumStorableCircle(cappedCaster));

        caster.Delete();
        cappedCaster.Delete();
    }

    [Fact]
    public void EligibleDefinitions_RequireRegistrationKnowledgeAndStorageRank()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();
        MysticSpellbook book = null;

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.SA;
            Initializer.Configure();
            book = AddMysticBook(caster, 677, 678, 679, 680, 682, 683, 684, 687, 688, 689, 690, 691);

            var definitions = SpellTriggerSpell.GetEligibleDefinitions(caster);
            var spellIds = new int[definitions.Count];

            for (var i = 0; i < definitions.Count; i++)
            {
                spellIds[i] = definitions[i].SpellId;
            }

            Assert.Equal([677, 678, 679, 680, 682, 683, 684, 687, 688], spellIds);
        }
        finally
        {
            book?.Delete();
            Core.Expansion = previousExpansion;
            ResetSpellRegistry();
            Initializer.Configure();
            caster.Delete();
        }
    }

    [Fact]
    public void CheckCast_EnforcesSaSkillManaAndEligibleSpellRequirements()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();
        var book = AddMysticBook(caster, 677);
        caster.MoveToWorld(new Point3D(100, 100, 0), Map.Felucca);

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.SA;
            Initializer.Configure();

            var spell = new SpellTriggerSpell(caster);
            Assert.True(spell.CheckCast());

            caster.Skills.Mysticism.Base = 44.9;
            Assert.False(spell.CheckCast());

            caster.Skills.Mysticism.Base = 120.0;
            caster.Mana = 0;
            Assert.False(spell.CheckCast());

            caster.Mana = caster.ManaMax;
            Core.Expansion = Expansion.ML;
            Assert.False(spell.CheckCast());
        }
        finally
        {
            book.Delete();
            caster.Delete();
            Core.Expansion = previousExpansion;
            ResetSpellRegistry();
            Initializer.Configure();
        }
    }

    [Fact]
    public void Selection_CreatesOneStoneReplacesPreviousStoneAndConsumesResources()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();
        var book = AddMysticBook(caster, 677);
        var previousStone = new SpellStone(684);
        caster.AddToBackpack(previousStone);
        AddReagents(caster);

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.SA;
            Initializer.Configure();

            var spell = BeginSelection(caster);
            spell.FinishSelection(677);

            var stone = caster.Backpack.FindItemByType<SpellStone>();
            Assert.NotNull(stone);
            Assert.Equal(677, stone.SpellId);
            Assert.True(previousStone.Deleted);
            Assert.Null(caster.Spell);
            Assert.Equal(SpellState.None, spell.State);
            Assert.Equal(86, caster.Mana);
            Assert.Equal(9, caster.Backpack.FindItemByType<DragonsBlood>().Amount);
            Assert.Equal(9, caster.Backpack.FindItemByType<Garlic>().Amount);
            Assert.Equal(9, caster.Backpack.FindItemByType<MandrakeRoot>().Amount);
            Assert.Equal(9, caster.Backpack.FindItemByType<SpidersSilk>().Amount);
        }
        finally
        {
            book.Delete();
            caster.Delete();
            Core.Expansion = previousExpansion;
            ResetSpellRegistry();
            Initializer.Configure();
        }
    }

    [Fact]
    public void SelectionCancel_DoesNotConsumeResourcesOrCreateStone()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();
        var book = AddMysticBook(caster, 677);
        AddReagents(caster);

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.SA;
            Initializer.Configure();

            var spell = BeginSelection(caster);
            var gump = new SpellTriggerGump(spell, [SpellTriggerSpell.Definitions[0]]);
            var relay = new RelayInfo(
                0,
                ReadOnlySpan<int>.Empty,
                ReadOnlySpan<ushort>.Empty,
                ReadOnlySpan<Range>.Empty,
                ReadOnlySpan<byte>.Empty
            );

            gump.OnResponse(null, in relay);

            Assert.Null(caster.Spell);
            Assert.Equal(SpellState.None, spell.State);
            Assert.Null(caster.Backpack.FindItemByType<SpellStone>());
            Assert.Equal(100, caster.Mana);
            Assert.Equal(10, caster.Backpack.FindItemByType<DragonsBlood>().Amount);
            Assert.Equal(10, caster.Backpack.FindItemByType<Garlic>().Amount);
            Assert.Equal(10, caster.Backpack.FindItemByType<MandrakeRoot>().Amount);
            Assert.Equal(10, caster.Backpack.FindItemByType<SpidersSilk>().Amount);
        }
        finally
        {
            book.Delete();
            caster.Delete();
            Core.Expansion = previousExpansion;
            ResetSpellRegistry();
            Initializer.Configure();
        }
    }

    [Fact]
    public void SelectionGumpResponse_CreatesSelectedStone()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();
        var book = AddMysticBook(caster, 677);
        AddReagents(caster);

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.SA;
            Initializer.Configure();

            var spell = BeginSelection(caster);
            var gump = new SpellTriggerGump(spell, [SpellTriggerSpell.Definitions[0]]);
            var relay = new RelayInfo(
                100,
                ReadOnlySpan<int>.Empty,
                ReadOnlySpan<ushort>.Empty,
                ReadOnlySpan<Range>.Empty,
                ReadOnlySpan<byte>.Empty
            );

            gump.OnResponse(null, in relay);

            Assert.Equal(677, caster.Backpack.FindItemByType<SpellStone>()?.SpellId);
            Assert.Null(caster.Spell);
        }
        finally
        {
            book.Delete();
            caster.Delete();
            Core.Expansion = previousExpansion;
            ResetSpellRegistry();
            Initializer.Configure();
        }
    }

    [Fact]
    public void SelectionGump_CompilesWithVisibleLayout()
    {
        var caster = NewCaster();
        var spell = new SpellTriggerSpell(caster);
        var gump = new SpellTriggerGump(spell, SpellTriggerSpell.Definitions);
        var buffer = GC.AllocateUninitializedArray<byte>(4096);
        var writer = new SpanWriter(buffer);
        gump.Compile(ref writer);

        Assert.True(writer.BytesWritten > 0);

        caster.Delete();
    }

    [Fact]
    public void SpellStoneActivation_UsesNormalSpellCastAndStartsCooldown()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();
        var book = AddMysticBook(caster, 677);
        var stone = new SpellStone(677);
        var target = NewCaster();
        caster.AddToBackpack(stone);
        caster.MoveToWorld(new Point3D(100, 100, 0), Map.Felucca);
        target.MoveToWorld(new Point3D(101, 100, 0), Map.Felucca);

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.SA;
            Initializer.Configure();

            Assert.True(stone.TryUseForTests(caster));
            Assert.True(stone.Deleted);
            Assert.True(SpellTriggerSpell.IsOnCooldown(caster, out var remaining));
            Assert.InRange(remaining.TotalSeconds, 299, SpellTriggerSpell.CooldownSeconds);

            var spell = Assert.IsType<NetherBoltSpell>(caster.Spell);
            spell.State = SpellState.Sequencing;
            spell.OnCast();
            Assert.NotNull(caster.Target);
            spell.Target(target);
            spell.FinishSequence();
        }
        finally
        {
            SpellTriggerSpell.ClearCooldownForTests(caster);
            book.Delete();
            caster.Delete();
            target.Delete();
            Core.Expansion = previousExpansion;
            ResetSpellRegistry();
            Initializer.Configure();
        }
    }

    [Fact]
    public void SpellStoneActivation_UsesNormalSelfAndNoTargetSpellPipeline()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();
        var book = AddMysticBook(caster, 678);
        var stone = new SpellStone(678);
        caster.AddToBackpack(stone);
        caster.AddToBackpack(new Bone(10));
        caster.AddToBackpack(new Garlic(10));
        caster.AddToBackpack(new Ginseng(10));
        caster.AddToBackpack(new SpidersSilk(10));
        caster.MoveToWorld(new Point3D(100, 100, 0), Map.Felucca);

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.SA;
            Initializer.Configure();

            Assert.True(stone.TryUseForTests(caster));
            var spell = Assert.IsType<HealingStoneSpell>(caster.Spell);
            spell.State = SpellState.Sequencing;
            spell.OnCast();

            Assert.Null(caster.Target);
            Assert.Null(caster.Spell);
            Assert.Equal(10, caster.Backpack.FindItemByType<Bone>().Amount);
            Assert.Equal(10, caster.Backpack.FindItemByType<Garlic>().Amount);
            Assert.Equal(10, caster.Backpack.FindItemByType<Ginseng>().Amount);
            Assert.Equal(10, caster.Backpack.FindItemByType<SpidersSilk>().Amount);
        }
        finally
        {
            SpellTriggerSpell.ClearCooldownForTests(caster);
            book.Delete();
            caster.Delete();
            Core.Expansion = previousExpansion;
            ResetSpellRegistry();
            Initializer.Configure();
        }
    }

    [Fact]
    public void SpellStoneActivation_RejectsUnknownStoredSpellAndHonorsCooldown()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();
        var stone = new SpellStone(677);
        caster.AddToBackpack(stone);
        var secondStone = new SpellStone(677);
        caster.AddToBackpack(secondStone);

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.SA;
            Initializer.Configure();

            Assert.False(stone.TryUseForTests(caster));
            Assert.False(stone.Deleted);

            var book = AddMysticBook(caster, 677);
            Assert.True(stone.TryUseForTests(caster));
            caster.Spell?.FinishSequence();
            Assert.True(stone.Deleted);

            Assert.False(secondStone.TryUseForTests(caster));
            Assert.False(secondStone.Deleted);

            EventSink.InvokeLogout(caster);
            Assert.True(secondStone.TryUseForTests(caster));
            caster.Spell?.FinishSequence();
            book.Delete();
        }
        finally
        {
            SpellTriggerSpell.ClearCooldownForTests(caster);
            caster.Delete();
            Core.Expansion = previousExpansion;
            ResetSpellRegistry();
            Initializer.Configure();
        }
    }

    [Fact]
    public void SpellStone_IsBlessedNontransferableAndDestroyedWhenDropped()
    {
        var owner = NewCaster();
        var other = NewCaster();
        var stone = new SpellStone(677);
        owner.AddToBackpack(stone);

        Assert.Equal(LootType.Blessed, stone.LootType);
        Assert.True(stone.Nontransferable);
        Assert.False(stone.AllowSecureTrade(owner, other, owner, true));
        Assert.False(stone.DropToWorld(owner, Point3D.Zero));
        Assert.True(stone.Deleted);

        owner.Delete();
        other.Delete();
    }

    private static PlayerMobile NewCaster(double mysticism = 120.0, double focus = 120.0, double imbuing = 0.0)
    {
        var caster = new PlayerMobile(World.NewMobile);
        caster.DefaultMobileInit();
        caster.Player = true;
        caster.InitStats(100, 100, 100);
        caster.Mana = caster.ManaMax;
        caster.AddItem(new Backpack());
        caster.Skills.Mysticism.Base = mysticism;
        caster.Skills.Focus.Base = focus;
        caster.Skills.Imbuing.Base = imbuing;
        return caster;
    }

    private static TestSpellTriggerSpell BeginSelection(PlayerMobile caster)
    {
        var spell = new TestSpellTriggerSpell(caster);
        caster.Spell = spell;
        spell.State = SpellState.Sequencing;
        return spell;
    }

    private static MysticSpellbook AddMysticBook(PlayerMobile caster, params int[] spellIds)
    {
        ulong content = 0;

        for (var i = 0; i < spellIds.Length; i++)
        {
            content |= 1UL << (spellIds[i] - 677);
        }

        var book = new MysticSpellbook(content);
        caster.AddToBackpack(book);
        return book;
    }

    private static void AddReagents(Mobile caster)
    {
        caster.AddToBackpack(new DragonsBlood(10));
        caster.AddToBackpack(new Garlic(10));
        caster.AddToBackpack(new MandrakeRoot(10));
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

    private sealed class TestSpellTriggerSpell : SpellTriggerSpell
    {
        public TestSpellTriggerSpell(Mobile caster) : base(caster)
        {
        }

        public override bool CheckFizzle() => true;
    }
}
