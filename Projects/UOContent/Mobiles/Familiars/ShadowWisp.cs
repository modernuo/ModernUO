using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Mobiles
{
  public class ShadowWispFamiliar : BaseFamiliar
  {
    private DateTime m_NextFlare;

    public ShadowWispFamiliar()
    {
      Body = 165;
      Hue = 0x901;
      BaseSoundID = 466;

      SetStr(50);
      SetDex(60);
      SetInt(100);

      SetHits(50);
      SetStam(60);
      SetMana(0);

      SetDamage(5, 10);

      SetDamageType(ResistanceType.Energy, 100);

      SetResistance(ResistanceType.Physical, 10, 15);
      SetResistance(ResistanceType.Fire, 10, 15);
      SetResistance(ResistanceType.Cold, 10, 15);
      SetResistance(ResistanceType.Poison, 10, 15);
      SetResistance(ResistanceType.Energy, 99);

      SetSkill(SkillName.Wrestling, 40.0);
      SetSkill(SkillName.Tactics, 40.0);

      ControlSlots = 1;
    }

    public ShadowWispFamiliar(Serial serial) : base(serial)
    {
    }

    public override string CorpseName => "a shadow wisp corpse";
    public override string DefaultName => "a shadow wisp";

    public override void OnThink()
    {
      base.OnThink();

      if (DateTime.UtcNow < m_NextFlare)
        return;

      m_NextFlare = DateTime.UtcNow + TimeSpan.FromSeconds(5.0 + 25.0 * Utility.RandomDouble());

      FixedEffect(0x37C4, 1, 12, 1109, 6);
      PlaySound(0x1D3);

      Timer.DelayCall(TimeSpan.FromSeconds(0.5), Flare);
    }

    private void Flare()
    {
      Mobile caster = ControlMaster ?? SummonMaster;

      if (caster == null)
        return;

      List<Mobile> list = GetMobilesInRange(5).Where(m =>
        m.Player && m.Alive && !m.IsDeadBondedPet && m.Karma <= 0 && m.AccessLevel < AccessLevel.Counselor).ToList();

      for (int i = 0; i < list.Count; ++i)
      {
        Mobile m = list[i];
        bool friendly = true;

        for (int j = 0; friendly && j < caster.Aggressors.Count; ++j)
          friendly = caster.Aggressors[j].Attacker != m;

        for (int j = 0; friendly && j < caster.Aggressed.Count; ++j)
          friendly = caster.Aggressed[j].Defender != m;

        if (friendly)
        {
          m.FixedEffect(0x37C4, 1, 12, 1109, 3); // At player
          m.Mana += 1 - m.Karma / 1000;
        }
      }
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}
