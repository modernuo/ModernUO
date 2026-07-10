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
using Server.Spells.Ninjitsu;
using Server.Tests;
using Xunit;

namespace Server.Tests.Spells.Mysticism;

[Collection("Sequential UOContent Tests")]
public class EnchantSpellTests
{
    [Fact]
    public void SpellMetadata_MatchesEnchantSources()
    {
        var caster = NewCaster();
        var spell = new EnchantSpell(caster);

        Assert.Equal("Enchant", spell.Name);
        Assert.Equal("In Ort Ylem", spell.Mantra);
        Assert.Equal(SpellCircle.Second, spell.Circle);
        Assert.Equal(TimeSpan.FromSeconds(0.75), spell.CastDelayBase);
        Assert.Equal(6, spell.GetMana());
        Assert.Equal(8.0, spell.RequiredSkill);
        Assert.Equal(SkillName.Mysticism, spell.CastSkill);
        Assert.Equal([Reagent.SpidersSilk, Reagent.MandrakeRoot, Reagent.SulfurousAsh], spell.Reagents);
        Assert.False(spell.ClearHandsOnCast);

        caster.Delete();
    }

    [Fact]
    public void MysticSpellbookAndScroll_UseEnchantSlot()
    {
        var book = new MysticSpellbook(1UL << (680 - 677));
        var scroll = new EnchantScroll();

        Assert.Equal(677, book.BookOffset);
        Assert.Equal(16, book.BookCount);
        Assert.True(book.HasSpell(680));
        Assert.Equal(680, GetSpellScrollId(scroll));

        book.Delete();
        scroll.Delete();
    }

    [Fact]
    public void RegisterMysticism_PreSaDoesNotExposeEnchant_AndSaUsesSpellId680()
    {
        var previousExpansion = Core.Expansion;
        var caster = NewCaster();

        try
        {
            ResetSpellRegistry();
            Core.Expansion = Expansion.ML;
            Initializer.Configure();

            Assert.Null(SpellRegistry.NewSpell(680, caster, null));
            Assert.Equal(-1, SpellRegistry.GetRegistryNumber(typeof(EnchantSpell)));

            ResetSpellRegistry();
            Core.Expansion = Expansion.SA;
            Initializer.Configure();

            Assert.Same(typeof(EnchantSpell), SpellRegistry.Types[680]);
            Assert.Equal(680, SpellRegistry.GetRegistryNumber(typeof(EnchantSpell)));
            Assert.IsType<EnchantSpell>(SpellRegistry.NewSpell(680, caster, null));
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
    public void Duration_ScalesWithSelectedHitSpellLevelAndCapsAt150Seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), EnchantSpell.GetDuration(AosWeaponAttribute.HitMagicArrow));
        Assert.Equal(TimeSpan.FromSeconds(60), EnchantSpell.GetDuration(AosWeaponAttribute.HitHarm));
        Assert.Equal(TimeSpan.FromSeconds(90), EnchantSpell.GetDuration(AosWeaponAttribute.HitFireball));
        Assert.Equal(TimeSpan.FromSeconds(120), EnchantSpell.GetDuration(AosWeaponAttribute.HitLightning));
        Assert.Equal(TimeSpan.FromSeconds(150), EnchantSpell.GetDuration(AosWeaponAttribute.HitDispel));
    }

    [Fact]
    public void HitSpellChance_UsesHigherFocusOrImbuingAndCapsAt60()
    {
        var focusCaster = NewCaster(120, 120, 0);
        var imbuingCaster = NewCaster(120, 0, 120);
        var lowCaster = NewCaster(80, 0, 0);

        Assert.Equal(60, EnchantSpell.GetHitSpellChance(focusCaster));
        Assert.Equal(60, EnchantSpell.GetHitSpellChance(imbuingCaster));
        var lowExpected = (int)(60 * (lowCaster.Skills.Mysticism.Value +
                                       Math.Max(lowCaster.Skills.Focus.Value, lowCaster.Skills.Imbuing.Value)) / 240.0);
        Assert.Equal(lowExpected, EnchantSpell.GetHitSpellChance(lowCaster));

        focusCaster.Delete();
        imbuingCaster.Delete();
        lowCaster.Delete();
    }

    [Fact]
    public void Selection_AppliesRuntimeOnlyHitSpellAndChannelingState()
    {
        var previousExpansion = Core.Expansion;
        Core.Expansion = Expansion.SA;

        var caster = NewCaster();
        var weapon = new Katana();
        AddReagents(caster);
        Assert.True(caster.EquipItem(weapon));

        try
        {
            var spell = BeginSelection(caster);
            var baselineCastDelay = new NetherBoltSpell(caster).GetCastDelay();
            spell.FinishSelection(weapon, AosWeaponAttribute.HitLightning);

            Assert.Null(caster.Spell);
            Assert.Equal(SpellState.None, spell.State);
            Assert.True(EnchantSpell.IsEnchanted(weapon));
            Assert.Equal(60, EnchantSpell.GetHitSpellBonus(weapon, AosWeaponAttribute.HitLightning));
            Assert.Equal(0, weapon.WeaponAttributes.HitLightning);
            Assert.Equal(0, weapon.Attributes.SpellChanneling);
            Assert.True(EnchantSpell.ProvidesSpellChanneling(weapon, caster));
            Assert.Equal(-1, EnchantSpell.GetFasterCasting(caster));
            Assert.Equal(TimeSpan.FromSeconds(0.25), new NetherBoltSpell(caster).GetCastDelay() - baselineCastDelay);
            Assert.True(weapon.AllowEquippedCast(caster));
            Assert.Equal(94, caster.Mana);
            Assert.Equal(9, caster.Backpack.FindItemByType<SpidersSilk>().Amount);
            Assert.Equal(9, caster.Backpack.FindItemByType<MandrakeRoot>().Amount);
            Assert.Equal(9, caster.Backpack.FindItemByType<SulfurousAsh>().Amount);

            caster.RemoveItem(weapon);

            Assert.False(EnchantSpell.IsEnchanted(weapon));
            Assert.Equal(0, EnchantSpell.GetHitSpellBonus(weapon, AosWeaponAttribute.HitLightning));
            Assert.Equal(0, EnchantSpell.GetFasterCasting(caster));

            weapon.Attributes.SpellChanneling = 1;
            AddReagents(caster);
            Assert.True(caster.EquipItem(weapon));
            var permanentChannelingSpell = BeginSelection(caster);
            permanentChannelingSpell.FinishSelection(weapon, AosWeaponAttribute.HitHarm);

            Assert.False(EnchantSpell.ProvidesSpellChanneling(weapon, caster));
            Assert.Equal(-1, EnchantSpell.GetFasterCasting(caster));
        }
        finally
        {
            EnchantSpell.StopEffect(weapon);
            weapon.Delete();
            caster.Delete();
            Core.Expansion = previousExpansion;
        }
    }

    [Fact]
    public void Selection_BelowThresholdDoesNotGrantTemporaryChannelingOrFasterCasting()
    {
        var previousExpansion = Core.Expansion;
        Core.Expansion = Expansion.SA;

        var caster = NewCaster(79, 120, 0);
        var weapon = new Katana();
        AddReagents(caster);
        Assert.True(caster.EquipItem(weapon));

        try
        {
            var spell = BeginSelection(caster);
            spell.FinishSelection(weapon, AosWeaponAttribute.HitMagicArrow);

            Assert.True(EnchantSpell.IsEnchanted(weapon));
            Assert.False(EnchantSpell.ProvidesSpellChanneling(weapon, caster));
            Assert.Equal(0, EnchantSpell.GetFasterCasting(caster));
            var expected = (int)(60 * (caster.Skills.Mysticism.Value +
                                        Math.Max(caster.Skills.Focus.Value, caster.Skills.Imbuing.Value)) / 240.0);
            Assert.Equal(expected, EnchantSpell.GetHitSpellBonus(weapon, AosWeaponAttribute.HitMagicArrow));
        }
        finally
        {
            EnchantSpell.StopEffect(weapon);
            weapon.Delete();
            caster.Delete();
            Core.Expansion = previousExpansion;
        }
    }

    [Fact]
    public void Threshold_At80MysticismAndSupportSkillGrantsAdvancedEffects()
    {
        var previousExpansion = Core.Expansion;
        Core.Expansion = Expansion.SA;

        var caster = NewCaster(80, 80, 0);
        var weapon = new Katana();
        AddReagents(caster);

        try
        {
            Assert.True(caster.EquipItem(weapon));
            var spell = BeginSelection(caster);
            spell.FinishSelection(weapon, AosWeaponAttribute.HitMagicArrow);

            Assert.True(EnchantSpell.ProvidesSpellChanneling(weapon, caster));
            Assert.Equal(-1, EnchantSpell.GetFasterCasting(caster));
        }
        finally
        {
            EnchantSpell.StopEffect(weapon);
            weapon.Delete();
            caster.Delete();
            Core.Expansion = previousExpansion;
        }
    }

    [Fact]
    public void CheckCast_RejectsExistingHitSpellAndIncompatibleEnchantments()
    {
        var previousExpansion = Core.Expansion;
        Core.Expansion = Expansion.SA;

        var caster = NewCaster();
        var weapon = new Katana();
        Assert.True(caster.EquipItem(weapon));

        try
        {
            var spell = new EnchantSpell(caster);
            weapon.WeaponAttributes.HitFireball = 1;
            Assert.False(spell.CheckCast());

            weapon.WeaponAttributes.HitFireball = 0;
            weapon.Consecrated = true;
            Assert.False(spell.CheckCast());

            weapon.Consecrated = false;
            caster.Skills.Ninjitsu.Base = 120.0;
            SpecialMove.Table[caster] = new FocusAttack();
            Assert.False(spell.CheckCast());
            SpecialMove.Table.Remove(caster);
        }
        finally
        {
            SpecialMove.Table.Remove(caster);
            weapon.Delete();
            caster.Delete();
            Core.Expansion = previousExpansion;
        }
    }

    [Fact]
    public void Expiry_RemovesTemporaryStateAndCasterLifecycleBreaksEffect()
    {
        var previousExpansion = Core.Expansion;
        Core.Expansion = Expansion.SA;

        var caster = NewCaster();
        var weapon = new Katana();
        AddReagents(caster);
        Assert.True(caster.EquipItem(weapon));

        try
        {
            var spell = BeginSelection(caster);
            spell.FinishSelection(weapon, AosWeaponAttribute.HitDispel);
            Assert.True(EnchantSpell.IsEnchanted(weapon));

            EnchantSpell.ExpireForTests(weapon);
            Assert.False(EnchantSpell.IsEnchanted(weapon));
            Assert.Equal(0, EnchantSpell.GetHitSpellBonus(weapon, AosWeaponAttribute.HitDispel));
            Assert.Equal(0, EnchantSpell.GetFasterCasting(caster));

            spell = BeginSelection(caster);
            spell.FinishSelection(weapon, AosWeaponAttribute.HitHarm);
            Assert.True(EnchantSpell.IsEnchanted(weapon));

            caster.RemoveItem(weapon);
            Assert.False(EnchantSpell.IsEnchanted(weapon));

            Assert.True(caster.EquipItem(weapon));
            spell = BeginSelection(caster);
            spell.FinishSelection(weapon, AosWeaponAttribute.HitHarm);
            Assert.True(EnchantSpell.IsEnchanted(weapon));

            EnchantSpell.Configure();
            EventSink.InvokeLogout(caster);
            Assert.False(EnchantSpell.IsEnchanted(weapon));

            spell = BeginSelection(caster);
            spell.FinishSelection(weapon, AosWeaponAttribute.HitHarm);
            Assert.True(EnchantSpell.IsEnchanted(weapon));

            PlayerMobile.PlayerDeletedEvent(caster);
            Assert.False(EnchantSpell.IsEnchanted(weapon));
            Assert.Equal(0, EnchantSpell.GetHitSpellBonus(weapon, AosWeaponAttribute.HitHarm));
        }
        finally
        {
            EnchantSpell.StopEffect(weapon);
            weapon.Delete();
            caster.Delete();
            Core.Expansion = previousExpansion;
        }
    }

    [Fact]
    public void OnHit_UsesTemporaryHitSpellBonus()
    {
        var previousExpansion = Core.Expansion;
        using var random = new PredictableRandom(0);
        Core.Expansion = Expansion.SA;

        var caster = NewCaster();
        caster.MoveToWorld(new Point3D(6200, 520, 0), Map.Felucca);
        var weapon = new TestEnchantKatana();
        var baselineDefender = NewCombatMobile(new Point3D(6201, 520, 0));
        var enchantedDefender = NewCombatMobile(new Point3D(6201, 520, 0));
        AddReagents(caster);

        try
        {
            Assert.True(caster.EquipItem(weapon));
            var baselineHits = baselineDefender.Hits;
            weapon.OnHit(caster, baselineDefender);
            var baselineDamage = baselineHits - baselineDefender.Hits;

            var spell = BeginSelection(caster);
            spell.FinishSelection(weapon, AosWeaponAttribute.HitMagicArrow);

            weapon.OnHit(caster, enchantedDefender);

            Assert.Equal(1, baselineDamage);
            Assert.Equal(1, weapon.MagicArrowCount);
        }
        finally
        {
            EnchantSpell.StopEffect(weapon);
            weapon.Delete();
            baselineDefender.Delete();
            enchantedDefender.Delete();
            caster.Delete();
            Core.Expansion = previousExpansion;
        }
    }

    [Fact]
    public void SelectionGumpResponse_AppliesSelectedEffect()
    {
        var previousExpansion = Core.Expansion;
        Core.Expansion = Expansion.SA;

        var caster = NewCaster();
        var weapon = new Katana();
        AddReagents(caster);

        try
        {
            Assert.True(caster.EquipItem(weapon));
            var spell = BeginSelection(caster);
            var gump = new EnchantGump(spell, weapon);
            var relay = new RelayInfo(
                1,
                ReadOnlySpan<int>.Empty,
                ReadOnlySpan<ushort>.Empty,
                ReadOnlySpan<Range>.Empty,
                ReadOnlySpan<byte>.Empty
            );

            gump.OnResponse(null, in relay);

            Assert.True(EnchantSpell.IsEnchanted(weapon));
            Assert.Equal(60, EnchantSpell.GetHitSpellBonus(weapon, AosWeaponAttribute.HitLightning));
        }
        finally
        {
            EnchantSpell.StopEffect(weapon);
            weapon.Delete();
            caster.Delete();
            Core.Expansion = previousExpansion;
        }
    }

    [Fact]
    public void SelectionCancel_DoesNotConsumeManaOrReagents()
    {
        var previousExpansion = Core.Expansion;
        Core.Expansion = Expansion.SA;

        var caster = NewCaster();
        var weapon = new Katana();
        AddReagents(caster);

        try
        {
            Assert.True(caster.EquipItem(weapon));
            var spell = BeginSelection(caster);
            var gump = new EnchantGump(spell, weapon);
            var relay = new RelayInfo(
                0,
                ReadOnlySpan<int>.Empty,
                ReadOnlySpan<ushort>.Empty,
                ReadOnlySpan<Range>.Empty,
                ReadOnlySpan<byte>.Empty
            );

            gump.OnResponse(null, in relay);

            Assert.Null(caster.Spell);
            Assert.False(EnchantSpell.IsEnchanted(weapon));
            Assert.Equal(100, caster.Mana);
            Assert.Equal(10, caster.Backpack.FindItemByType<SpidersSilk>().Amount);
            Assert.Equal(10, caster.Backpack.FindItemByType<MandrakeRoot>().Amount);
            Assert.Equal(10, caster.Backpack.FindItemByType<SulfurousAsh>().Amount);
        }
        finally
        {
            EnchantSpell.StopEffect(weapon);
            weapon.Delete();
            caster.Delete();
            Core.Expansion = previousExpansion;
        }
    }

    [Fact]
    public void SelectionGump_CompilesWithVisibleLayout()
    {
        var caster = NewCaster();
        var weapon = new Katana();
        var spell = new EnchantSpell(caster);
        var gump = new EnchantGump(spell, weapon);
        var buffer = GC.AllocateUninitializedArray<byte>(2048);
        var writer = new SpanWriter(buffer);
        gump.Compile(ref writer);

        Assert.True(writer.BytesWritten > 0);

        weapon.Delete();
        caster.Delete();
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

    private static Mobile NewCombatMobile(Point3D location)
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.InitStats(100, 100, 100);
        mobile.Hits = mobile.HitsMax;
        mobile.MoveToWorld(location, Map.Felucca);
        return mobile;
    }

    private static TestEnchantSpell BeginSelection(PlayerMobile caster)
    {
        var spell = new TestEnchantSpell(caster);
        caster.Spell = spell;
        spell.State = SpellState.Sequencing;
        return spell;
    }

    private static void AddReagents(Mobile caster)
    {
        caster.AddToBackpack(new SpidersSilk(10));
        caster.AddToBackpack(new MandrakeRoot(10));
        caster.AddToBackpack(new SulfurousAsh(10));
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

    private sealed class TestEnchantKatana : Katana
    {
        public int MagicArrowCount { get; private set; }

        public override bool CheckHit(Mobile attacker, Mobile defender) => true;

        public override int ComputeDamage(Mobile attacker, Mobile defender) => 1;

        public override void DoMagicArrow(Mobile attacker, Mobile defender)
        {
            MagicArrowCount++;
            base.DoMagicArrow(attacker, defender);
        }

        public override int AbsorbDamage(Mobile attacker, Mobile defender, int damage) => damage;

        public override void AddBlood(Mobile attacker, Mobile defender, int damage)
        {
        }
    }

    private sealed class TestEnchantSpell : EnchantSpell
    {
        public TestEnchantSpell(Mobile caster) : base(caster)
        {
        }

        public override bool CheckFizzle() => true;
    }
}
