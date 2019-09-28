using Server.Items;

namespace Server.Mobiles
{
  public class ChaosDragoonElite : BaseCreature
  {
    [Constructible]
    public ChaosDragoonElite()
      : base(AIType.AI_Mage, FightMode.Closest, 10, 1, 0.15, 0.4)
    {
      Body = 0x190;
      Hue = Race.Human.RandomSkinHue();

      SetStr(276, 350);
      SetDex(66, 90);
      SetInt(126, 150);

      SetHits(276, 350);

      SetDamage(29, 34);

      SetDamageType(ResistanceType.Physical, 100);

      /*SetResistance(ResistanceType.Physical, 45, 55);
      SetResistance(ResistanceType.Fire, 15, 25);
      SetResistance(ResistanceType.Cold, 50);
      SetResistance(ResistanceType.Poison, 25, 35);
      SetResistance(ResistanceType.Energy, 25, 35);*/


      SetSkill(SkillName.Tactics, 80.1, 100.0);
      SetSkill(SkillName.MagicResist, 100.1, 110.0);
      SetSkill(SkillName.Anatomy, 80.1, 100.0);
      SetSkill(SkillName.Magery, 85.1, 100.0);
      SetSkill(SkillName.EvalInt, 85.1, 100.0);
      SetSkill(SkillName.Swords, 72.5, 95.0);
      SetSkill(SkillName.Fencing, 85.1, 100);
      SetSkill(SkillName.Macing, 85.1, 100);

      Fame = 8000;
      Karma = -8000;

      CraftResource res = Utility.Random(6) switch
      {
        0 => CraftResource.BlackScales,
        1 => CraftResource.RedScales,
        2 => CraftResource.BlueScales,
        3 => CraftResource.YellowScales,
        4 => CraftResource.GreenScales,
        5 => CraftResource.WhiteScales,
        _ => CraftResource.None
      };

      BaseWeapon melee = Utility.Random(3) switch
      {
        0 => (BaseWeapon)new Kryss(),
        1 => new Broadsword(),
        2 => new Katana(),
        _ => null
      };

      melee.Movable = false;
      AddItem(melee);

      DragonChest Tunic = new DragonChest();
      Tunic.Resource = res;
      Tunic.Movable = false;
      AddItem(Tunic);

      DragonLegs Legs = new DragonLegs();
      Legs.Resource = res;
      Legs.Movable = false;
      AddItem(Legs);

      DragonArms Arms = new DragonArms();
      Arms.Resource = res;
      Arms.Movable = false;
      AddItem(Arms);

      DragonGloves Gloves = new DragonGloves();
      Gloves.Resource = res;
      Gloves.Movable = false;
      AddItem(Gloves);

      DragonHelm Helm = new DragonHelm();
      Helm.Resource = res;
      Helm.Movable = false;
      AddItem(Helm);

      ChaosShield shield = new ChaosShield();
      shield.Movable = false;
      AddItem(shield);

      AddItem(new Boots(0x455));
      AddItem(new Shirt(Utility.RandomMetalHue()));

      int amount = Utility.RandomMinMax(1, 3);

      switch (res)
      {
        case CraftResource.BlackScales:
          AddItem(new BlackScales(amount));
          break;
        case CraftResource.RedScales:
          AddItem(new RedScales(amount));
          break;
        case CraftResource.BlueScales:
          AddItem(new BlueScales(amount));
          break;
        case CraftResource.YellowScales:
          AddItem(new YellowScales(amount));
          break;
        case CraftResource.GreenScales:
          AddItem(new GreenScales(amount));
          break;
        case CraftResource.WhiteScales:
          AddItem(new WhiteScales(amount));
          break;
      }

      res = Utility.Random(9) switch
      {
        0 => CraftResource.DullCopper,
        1 => CraftResource.ShadowIron,
        2 => CraftResource.Copper,
        3 => CraftResource.Bronze,
        4 => CraftResource.Gold,
        5 => CraftResource.Agapite,
        6 => CraftResource.Verite,
        7 => CraftResource.Valorite,
        8 => CraftResource.Iron,
        _ => res
      };

      SwampDragon mt = new SwampDragon();
      mt.HasBarding = true;
      mt.BardingResource = res;
      mt.BardingHP = mt.BardingMaxHP;
      mt.Rider = this;
    }

    public ChaosDragoonElite(Serial serial)
      : base(serial)
    {
    }

    public override string CorpseName => "a chaos dragoon elite corpse";
    public override string DefaultName => "a chaos dragoon elite";

    public override bool HasBreath => true;
    public override bool AutoDispel => true;
    public override bool BardImmune => !Core.AOS;
    public override bool CanRummageCorpses => true;
    public override bool AlwaysMurderer => true;
    public override bool ShowFameTitle => false;

    public override int GetIdleSound() => 0x2CE;

    public override int GetDeathSound() => 0x2CC;

    public override int GetHurtSound() => 0x2D1;

    public override int GetAttackSound() => 0x2C8;

    public override void GenerateLoot()
    {
      AddLoot(LootPack.Rich);
      AddLoot(LootPack.Gems);
    }

    public override bool OnBeforeDeath()
    {
      IMount mount = Mount;

      if (mount != null)
      {
        if (mount is SwampDragon dragon)
          dragon.HasBarding = false;

        mount.Rider = null;
      }

      return base.OnBeforeDeath();
    }

    public override void AlterMeleeDamageTo(Mobile to, ref int damage)
    {
      if (to is Dragon || to is WhiteWyrm || to is SwampDragon || to is Drake || to is Nightmare || to is Hiryu ||
          to is LesserHiryu || to is Daemon)
        damage *= 3;
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);
      writer.Write(0);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);
      int version = reader.ReadInt();
    }
  }
}
