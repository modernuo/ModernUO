using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class RisingColossus : BaseCreature
{
    private double _dispelDifficulty;

    [Constructible]
    public RisingColossus(Mobile caster, double mysticism, double supportSkill) :
        base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        var level = (int)(mysticism + supportSkill);
        var statBonus = (int)((mysticism - 83.0) / 1.3 + (supportSkill - 30.0) / 1.3 + 6.0);
        var hitsBonus = (int)((mysticism - 83.0) * 1.14 + (supportSkill - 30.0) * 1.03 + 20.0);
        var skillValue = supportSkill != 0.0 ? (mysticism + supportSkill) / 2.0 : (mysticism + 20.0) / 2.0;

        Body = 829;

        SetStr(677 + statBonus);
        SetDex(107 + statBonus);
        SetInt(127 + statBonus);

        SetHits(315 + hitsBonus);
        SetStam(250);
        SetMana(0);
        SetDamage(level / 12, level / 10);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 65, 70);
        SetResistance(ResistanceType.Fire, 50, 55);
        SetResistance(ResistanceType.Cold, 50, 55);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 65, 70);

        SetSkill(SkillName.MagicResist, skillValue);
        SetSkill(SkillName.Tactics, skillValue);
        SetSkill(SkillName.Wrestling, skillValue);
        SetSkill(SkillName.Anatomy, skillValue);
        SetSkill(SkillName.EvalInt, skillValue);
        SetSkill(SkillName.DetectHidden, 70.0);
        SetSkill(SkillName.Mysticism, caster.Skills.Mysticism.Value);
        SetSkill(SkillName.Focus, caster.Skills.Focus.Value);
        SetSkill(SkillName.Imbuing, caster.Skills.Imbuing.Value);

        Fame = 0;
        Karma = 0;
        VirtualArmor = 60;
        ControlSlots = 5;

        // UO.com and ServUO document dispel counterplay but do not publish a
        // current numeric difficulty formula. Keep it in the normal 80-100
        // spell-skill range instead of inheriting ServUO's incompatible legacy
        // expression, which produces values above the current 0-200 scale.
        _dispelDifficulty = Math.Clamp(80.0 + (mysticism - 83.0) * 0.5, 80.0, 100.0);
    }

    public RisingColossus(Serial serial) : base(serial)
    {
    }

    [SerializableProperty(0, useField: nameof(_dispelDifficulty))]
    public override double DispelDifficulty => _dispelDifficulty;

    public override double DispelFocus => 45.0;
    public override string DefaultName => "a rising colossus";
    public override string CorpseName => "a rising colossus corpse";
    public override bool DeleteCorpseOnDeath => Summoned;
    public override bool AlwaysMurderer => true;
    public override bool BleedImmune => true;
    public override Poison PoisonImmune => Poison.Lethal;
    public override bool FollowsAcquireRules =>
        Core.AOS || !Summoned || SummonMaster?.Player != true || Map != Map.Felucca;

    public override double GetFightModeRanking(Mobile mobile, FightMode acqType, bool bPlayerOnly) =>
        (mobile.Int + mobile.Skills.Magery.Value) / Math.Max(this.GetDistanceToSqrt(mobile), 1.0);

    public override WeaponAbility GetWeaponAbility() =>
        Utility.RandomBool() ? WeaponAbility.ArmorIgnore : WeaponAbility.CrushingBlow;

    public override int GetAttackSound() => 0x627;
    public override int GetHurtSound() => 0x629;
}