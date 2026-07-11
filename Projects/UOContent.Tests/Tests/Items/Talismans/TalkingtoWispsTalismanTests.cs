using System;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Spells.Fifth;
using Server.Spells.First;
using Server.Spells.Second;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class TalkingtoWispsTalismanTests
{
    static TalkingtoWispsTalismanTests()
    {
        NotorietyHandlers.Initialize();
    }

    [Fact]
    public void Construction_UsesTheLibraryRewardIdentityAndFixedWardRemovalProperties()
    {
        var talisman = new TalkingtoWispsTalisman();

        try
        {
            Assert.Equal(1073356, talisman.LabelNumber);
            Assert.True(talisman.ForceShowName);
            Assert.Equal(0x2F5B, talisman.ItemID);
            Assert.Equal(1.0, talisman.Weight);
            Assert.Equal(TalismanRemoval.Ward, talisman.Removal);
            Assert.Equal(1200, talisman.MaxChargeTime);
            Assert.True(talisman.SkillBonuses.GetValues(0, out var firstSkill, out var firstBonus));
            Assert.Equal(SkillName.SpiritSpeak, firstSkill);
            Assert.Equal(3.0, firstBonus);
            Assert.True(talisman.SkillBonuses.GetValues(1, out var secondSkill, out var secondBonus));
            Assert.Equal(SkillName.EvalInt, secondSkill);
            Assert.Equal(5.0, secondBonus);
        }
        finally
        {
            talisman.Delete();
        }
    }

    [Fact]
    public void SerializationAndDuplication_PreserveTheConcreteTalismanProperties()
    {
        var talisman = new TalkingtoWispsTalisman();
        var dupe = new TalkingtoWispsTalisman();
        var serialized = new TalkingtoWispsTalisman();

        try
        {
            talisman.Blessed = true;
            talisman.Dupe(dupe);

            var writer = new BufferWriter(1024, false);
            talisman.Serialize(writer);
            var bytes = writer.Buffer.AsSpan(0, (int)writer.Position).ToArray();
            serialized.Deserialize(new BufferReader(bytes));

            AssertTalismanProperties(dupe);
            AssertTalismanProperties(serialized);
        }
        finally
        {
            talisman.Delete();
            dupe.Delete();
            serialized.Delete();
        }
    }

    [Theory]
    [InlineData(WardType.MagicReflection)]
    [InlineData(WardType.ReactiveArmor)]
    [InlineData(WardType.Protection)]
    public void Use_RemovesEachSupportedWardAndPreservesAnUnrelatedStatBuff(WardType wardType)
    {
        var caster = CreatePlayer(Map.Felucca);
        var target = CreatePlayer(Map.Felucca);
        var talisman = Equip(caster);

        try
        {
            ApplyWard(wardType, target);
            target.AddStatMod(new StatMod(StatType.Str, "TalkingtoWispsTalismanTests.Bless", 10, TimeSpan.FromMinutes(1)));

            Use(caster, talisman, target);

            Assert.False(HasWard(wardType, target));
            Assert.NotNull(target.GetStatMod("TalkingtoWispsTalismanTests.Bless"));
            Assert.Equal(1200, talisman.ChargeTime);
        }
        finally
        {
            ClearWards(target);
            talisman.Delete();
            caster.Delete();
            target.Delete();
        }
    }

    [Fact]
    public void Use_WithoutSupportedWardStillUsesTheEstablishedRechargePath()
    {
        var caster = CreatePlayer(Map.Felucca);
        var target = CreatePlayer(Map.Felucca);
        var talisman = Equip(caster);

        try
        {
            Use(caster, talisman, target);

            Assert.Equal(1200, talisman.ChargeTime);
            Assert.Equal(0, talisman.Charges);
        }
        finally
        {
            talisman.Delete();
            caster.Delete();
            target.Delete();
        }
    }

    [Fact]
    public void Use_DoesNotRemoveAPlayersWardInTrammelWhenTheTargetIsFriendly()
    {
        var caster = CreatePlayer(Map.Trammel);
        var target = CreatePlayer(Map.Trammel);
        var talisman = Equip(caster);

        try
        {
            ProtectionSpell.Toggle(target, target);
            Assert.True(ProtectionSpell.HasEffect(target));
            Assert.Equal(Notoriety.Innocent, Notoriety.Compute(caster, target));

            Use(caster, talisman, target);

            Assert.True(ProtectionSpell.HasEffect(target));
            Assert.Equal(0, talisman.ChargeTime);
        }
        finally
        {
            ClearWards(target);
            talisman.Delete();
            caster.Delete();
            target.Delete();
        }
    }

    [Fact]
    public void Use_RemovesAPlayersWardInFelucca()
    {
        var caster = CreatePlayer(Map.Felucca);
        var target = CreatePlayer(Map.Felucca);
        var talisman = Equip(caster);

        try
        {
            ProtectionSpell.Toggle(target, target);
            Assert.True(ProtectionSpell.HasEffect(target));

            Use(caster, talisman, target);

            Assert.False(ProtectionSpell.HasEffect(target));
            Assert.Equal(1200, talisman.ChargeTime);
        }
        finally
        {
            ClearWards(target);
            talisman.Delete();
            caster.Delete();
            target.Delete();
        }
    }

    [Fact]
    public void DoubleClick_UnequippedItemDoesNotOpenATarget()
    {
        var caster = CreatePlayer(Map.Felucca);
        var talisman = new TalkingtoWispsTalisman();

        try
        {
            talisman.OnDoubleClick(caster);

            Assert.Null(caster.Target);
        }
        finally
        {
            talisman.Delete();
            caster.Delete();
        }
    }

    private static void AssertTalismanProperties(TalkingtoWispsTalisman talisman)
    {
        Assert.True(talisman.Blessed);
        Assert.Equal(TalismanRemoval.Ward, talisman.Removal);
        Assert.Equal(1200, talisman.MaxChargeTime);
        Assert.True(talisman.SkillBonuses.GetValues(0, out var firstSkill, out var firstBonus));
        Assert.Equal(SkillName.SpiritSpeak, firstSkill);
        Assert.Equal(3.0, firstBonus);
        Assert.True(talisman.SkillBonuses.GetValues(1, out var secondSkill, out var secondBonus));
        Assert.Equal(SkillName.EvalInt, secondSkill);
        Assert.Equal(5.0, secondBonus);
    }

    private static TalkingtoWispsTalisman Equip(PlayerMobile caster)
    {
        var talisman = new TalkingtoWispsTalisman();
        Assert.True(caster.EquipItem(talisman));
        Assert.Same(talisman, caster.Talisman);
        return talisman;
    }

    private static PlayerMobile CreatePlayer(Map map)
    {
        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        player.Player = true;
        player.Body = 0x190;
        player.MoveToWorld(new Point3D(1500, 1600, map.GetAverageZ(1500, 1600)), map);
        player.AddItem(new Backpack());
        return player;
    }

    private static void Use(PlayerMobile caster, TalkingtoWispsTalisman talisman, Mobile target)
    {
        talisman.OnDoubleClick(caster);
        Assert.NotNull(caster.Target);
        caster.Target.Invoke(caster, target);
    }

    private static void ApplyWard(WardType wardType, Mobile target)
    {
        switch (wardType)
        {
            case WardType.MagicReflection:
                ApplyMagicReflection(target);
                break;
            case WardType.ReactiveArmor:
                ApplyReactiveArmor(target);
                break;
            case WardType.Protection:
                ProtectionSpell.Toggle(target, target);
                break;
        }
    }

    private static bool HasWard(WardType wardType, Mobile target) => wardType switch
    {
        WardType.MagicReflection => MagicReflectSpell.HasEffect(target),
        WardType.ReactiveArmor   => ReactiveArmorSpell.HasAosEffect(target),
        WardType.Protection      => ProtectionSpell.HasEffect(target),
        _                        => false
    };

    private static void ClearWards(Mobile target)
    {
        MagicReflectSpell.EndReflect(target);
        ReactiveArmorSpell.EndArmor(target);
        ProtectionSpell.EndProtection(target);
    }

    private static void ApplyReactiveArmor(Mobile target)
    {
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
            target.AddResistanceMod(mod);
        }

        var tableField = typeof(ReactiveArmorSpell).GetField("_table", BindingFlags.Static | BindingFlags.NonPublic)!;
        var table = (Dictionary<Mobile, ResistanceMod[]>?)tableField.GetValue(null) ?? [];
        table[target] = mods;
        tableField.SetValue(null, table);
    }

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

    public enum WardType
    {
        MagicReflection,
        ReactiveArmor,
        Protection
    }
}
