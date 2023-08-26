using System;
using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class DarkWolfFamiliar : BaseFamiliar
{
    private DateTime m_NextRestore;

    public DarkWolfFamiliar()
    {
        Body = 99;
        Hue = 0x901;
        BaseSoundID = 0xE5;

        SetStr(100);
        SetDex(90);
        SetInt(90);

        SetHits(60);
        SetStam(90);
        SetMana(0);

        SetDamage(5, 10);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 40, 50);
        SetResistance(ResistanceType.Fire, 25, 40);
        SetResistance(ResistanceType.Cold, 25, 40);
        SetResistance(ResistanceType.Poison, 25, 40);
        SetResistance(ResistanceType.Energy, 25, 40);

        SetSkill(SkillName.Wrestling, 85.1, 90.0);
        SetSkill(SkillName.Tactics, 50.0);

        ControlSlots = 1;
    }

    public override string CorpseName => "a dark wolf corpse";
    public override string DefaultName => "a dark wolf";

    public override void OnThink()
    {
        base.OnThink();

        if (Core.Now < m_NextRestore)
        {
            return;
        }

        m_NextRestore = Core.Now + TimeSpan.FromSeconds(2.0);

        var caster = ControlMaster ?? SummonMaster;

        if (caster != null)
        {
            ++caster.Stam;
        }
    }
}
