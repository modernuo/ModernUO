using System.Collections.Generic;

namespace Server.Mobiles
{
  public class SirPatrick : SkeletalKnight
  {
    [Constructible]
    public SirPatrick()
    {
      IsParagon = true;

      Hue = 0x47E;

      SetStr(208, 319);
      SetDex(98, 132);
      SetInt(45, 91);

      SetHits(616, 884);

      SetDamage(15, 25);

      SetDamageType(ResistanceType.Physical, 40);
      SetDamageType(ResistanceType.Cold, 60);

      SetResistance(ResistanceType.Physical, 55, 62);
      SetResistance(ResistanceType.Fire, 40, 48);
      SetResistance(ResistanceType.Cold, 71, 80);
      SetResistance(ResistanceType.Poison, 40, 50);
      SetResistance(ResistanceType.Energy, 50, 60);

      SetSkill(SkillName.Wrestling, 126.3, 136.5);
      SetSkill(SkillName.Tactics, 128.5, 143.8);
      SetSkill(SkillName.MagicResist, 102.8, 117.9);
      SetSkill(SkillName.Anatomy, 127.5, 137.2);

      Fame = 18000;
      Karma = -18000;
    }

    public SirPatrick(Serial serial)
      : base(serial)
    {
    }

    public override string CorpseName => "a Sir Patrick corpse";
    public override string DefaultName => "Sir Patrick";

    /*
    // TODO: uncomment once added
    public override void OnDeath( Container c )
    {
      base.OnDeath( c );

      if (Utility.RandomDouble() < 0.15)
        c.DropItem( new DisintegratingThesisNotes() );

      if (Utility.RandomDouble() < 0.05)
        c.DropItem( new AssassinChest() );
    }
    */

    public override bool GivesMLMinorArtifact => true;

    public override void GenerateLoot()
    {
      AddLoot(LootPack.UltraRich, 2);
    }

    public override void OnGaveMeleeAttack(Mobile defender)
    {
      base.OnGaveMeleeAttack(defender);

      if (Utility.RandomDouble() < 0.1)
        DrainLife();
    }

    public override void OnGotMeleeAttack(Mobile attacker)
    {
      base.OnGotMeleeAttack(attacker);

      if (Utility.RandomDouble() < 0.1)
        DrainLife();
    }

    public virtual void DrainLife()
    {
      List<Mobile> list = new List<Mobile>();

      foreach (Mobile m in GetMobilesInRange(2))
      {
        if (m == this || !CanBeHarmful(m, false) || (Core.AOS && !InLOS(m)))
          continue;

        if (m is BaseCreature bc)
        {
          if (bc.Controlled || bc.Summoned || bc.Team != Team)
            list.Add(bc);
        }
        else if (m.Player)
        {
          list.Add(m);
        }
      }

      foreach (Mobile m in list)
      {
        DoHarmful(m);

        m.FixedParticles(0x374A, 10, 15, 5013, 0x455, 0, EffectLayer.Waist);
        m.PlaySound(0x1EA);

        int drain = Utility.RandomMinMax(14, 30);

        Hits += drain;
        m.Damage(drain, this);
      }
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}
