using Server.Items;

namespace Server.Mobiles
{
  public class ChaosDaemon : BaseCreature
  {
    [Constructible]
    public ChaosDaemon() : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
    {
      Body = 792;
      BaseSoundID = 0x3E9;

      SetStr(106, 130);
      SetDex(171, 200);
      SetInt(56, 80);

      SetHits(91, 110);

      SetDamage(12, 17);

      SetDamageType(ResistanceType.Physical, 85);
      SetDamageType(ResistanceType.Fire, 15);

      SetResistance(ResistanceType.Physical, 50, 60);
      SetResistance(ResistanceType.Fire, 60, 70);
      SetResistance(ResistanceType.Cold, 40, 50);
      SetResistance(ResistanceType.Poison, 20, 30);
      SetResistance(ResistanceType.Energy, 20, 30);

      SetSkill(SkillName.MagicResist, 85.1, 95.0);
      SetSkill(SkillName.Tactics, 70.1, 80.0);
      SetSkill(SkillName.Wrestling, 95.1, 100.0);

      Fame = 3000;
      Karma = -4000;

      VirtualArmor = 15;
    }

    public ChaosDaemon(Serial serial) : base(serial)
    {
    }

    public override string CorpseName => "a chaos daemon corpse";

    public override string DefaultName => "a chaos daemon";

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.CrushingBlow;

    public override void GenerateLoot()
    {
      AddLoot(LootPack.Average);
      AddLoot(LootPack.Meager);
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