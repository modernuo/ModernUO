using Server.Items;
using Server.Spells.First;
using Server.Spells.Third;

namespace Server.Mobiles
{
  /// <summary>
  ///   This is a test creature
  ///   You can set its value in game
  ///   It die after 5 minutes, so your test server stay clean
  ///   Create a macro to help your creation "[add Dummy 1 15 7 -1 0.5 2"
  ///   A iTeam of negative will set a faction at random
  ///   Say Kill if you want them to die
  /// </summary>
  public class DummyMace : Dummy
  {
    [Constructible]
    public DummyMace() : base(AIType.AI_Melee, FightMode.Closest, 15, 1, 0.2, 0.6)
    {
      // A Dummy Macer
      int iHue = 20 + Team * 40;
      int jHue = 25 + Team * 40;

      // Skills and Stats
      InitStats(125, 125, 90);
      Skills.Macing.Base = 120;
      Skills.Anatomy.Base = 120;
      Skills.Healing.Base = 120;
      Skills.Tactics.Base = 120;

      // Equip
      WarHammer war = new WarHammer();
      war.Movable = true;
      war.Crafter = this;
      war.Quality = WeaponQuality.Regular;
      AddItem(war);

      Boots bts = new Boots();
      bts.Hue = iHue;
      AddItem(bts);

      ChainChest cht = new ChainChest();
      cht.Movable = false;
      cht.LootType = LootType.Newbied;
      cht.Crafter = this;
      cht.Quality = ArmorQuality.Regular;
      AddItem(cht);

      ChainLegs chl = new ChainLegs();
      chl.Movable = false;
      chl.LootType = LootType.Newbied;
      chl.Crafter = this;
      chl.Quality = ArmorQuality.Regular;
      AddItem(chl);

      PlateArms pla = new PlateArms();
      pla.Movable = false;
      pla.LootType = LootType.Newbied;
      pla.Crafter = this;
      pla.Quality = ArmorQuality.Regular;
      AddItem(pla);

      Bandage band = new Bandage(50);
      AddToBackpack(band);
    }

    public DummyMace(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "Macer";

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

  public class DummyFence : Dummy
  {
    [Constructible]
    public DummyFence() : base(AIType.AI_Melee, FightMode.Closest, 15, 1, 0.2, 0.6)
    {
      // A Dummy Fencer
      int iHue = 20 + Team * 40;
      int jHue = 25 + Team * 40;

      // Skills and Stats
      InitStats(125, 125, 90);
      Skills.Fencing.Base = 120;
      Skills.Anatomy.Base = 120;
      Skills.Healing.Base = 120;
      Skills.Tactics.Base = 120;

      // Equip
      Spear ssp = new Spear();
      ssp.Movable = true;
      ssp.Crafter = this;
      ssp.Quality = WeaponQuality.Regular;
      AddItem(ssp);

      Boots snd = new Boots();
      snd.Hue = iHue;
      snd.LootType = LootType.Newbied;
      AddItem(snd);

      ChainChest cht = new ChainChest();
      cht.Movable = false;
      cht.LootType = LootType.Newbied;
      cht.Crafter = this;
      cht.Quality = ArmorQuality.Regular;
      AddItem(cht);

      ChainLegs chl = new ChainLegs();
      chl.Movable = false;
      chl.LootType = LootType.Newbied;
      chl.Crafter = this;
      chl.Quality = ArmorQuality.Regular;
      AddItem(chl);

      PlateArms pla = new PlateArms();
      pla.Movable = false;
      pla.LootType = LootType.Newbied;
      pla.Crafter = this;
      pla.Quality = ArmorQuality.Regular;
      AddItem(pla);

      Bandage band = new Bandage(50);
      AddToBackpack(band);
    }

    public DummyFence(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "Fencer";

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

  public class DummySword : Dummy
  {
    [Constructible]
    public DummySword() : base(AIType.AI_Melee, FightMode.Closest, 15, 1, 0.2, 0.6)
    {
      // A Dummy Swordsman
      int iHue = 20 + Team * 40;
      int jHue = 25 + Team * 40;

      // Skills and Stats
      InitStats(125, 125, 90);
      Skills.Swords.Base = 120;
      Skills.Anatomy.Base = 120;
      Skills.Healing.Base = 120;
      Skills.Tactics.Base = 120;
      Skills.Parry.Base = 120;

      // Equip
      Katana kat = new Katana();
      kat.Crafter = this;
      kat.Movable = true;
      kat.Quality = WeaponQuality.Regular;
      AddItem(kat);

      Boots bts = new Boots();
      bts.Hue = iHue;
      AddItem(bts);

      ChainChest cht = new ChainChest();
      cht.Movable = false;
      cht.LootType = LootType.Newbied;
      cht.Crafter = this;
      cht.Quality = ArmorQuality.Regular;
      AddItem(cht);

      ChainLegs chl = new ChainLegs();
      chl.Movable = false;
      chl.LootType = LootType.Newbied;
      chl.Crafter = this;
      chl.Quality = ArmorQuality.Regular;
      AddItem(chl);

      PlateArms pla = new PlateArms();
      pla.Movable = false;
      pla.LootType = LootType.Newbied;
      pla.Crafter = this;
      pla.Quality = ArmorQuality.Regular;
      AddItem(pla);

      Bandage band = new Bandage(50);
      AddToBackpack(band);
    }

    public DummySword(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "Swordsman";

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

  public class DummyNox : Dummy
  {
    [Constructible]
    public DummyNox() : base(AIType.AI_Mage, FightMode.Closest, 15, 1, 0.2, 0.6)
    {
      // A Dummy Nox or Pure Mage
      int iHue = 20 + Team * 40;
      int jHue = 25 + Team * 40;

      // Skills and Stats
      InitStats(90, 90, 125);
      Skills.Magery.Base = 120;
      Skills.EvalInt.Base = 120;
      Skills.Inscribe.Base = 100;
      Skills.Wrestling.Base = 120;
      Skills.Meditation.Base = 120;
      Skills.Poisoning.Base = 100;

      // Equip
      Spellbook book = new Spellbook();
      book.Movable = false;
      book.LootType = LootType.Newbied;
      book.Content = 0xFFFFFFFFFFFFFFFF;
      AddItem(book);

      Kilt kilt = new Kilt();
      kilt.Hue = jHue;
      AddItem(kilt);

      Sandals snd = new Sandals();
      snd.Hue = iHue;
      snd.LootType = LootType.Newbied;
      AddItem(snd);

      SkullCap skc = new SkullCap();
      skc.Hue = iHue;
      AddItem(skc);

      // Spells
      AddSpellAttack(typeof(MagicArrowSpell));
      AddSpellAttack(typeof(WeakenSpell));
      AddSpellAttack(typeof(FireballSpell));
      AddSpellDefense(typeof(WallOfStoneSpell));
      AddSpellDefense(typeof(HealSpell));
    }

    public DummyNox(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "Nox Mage";

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

  public class DummyStun : Dummy
  {
    [Constructible]
    public DummyStun() : base(AIType.AI_Mage, FightMode.Closest, 15, 1, 0.2, 0.6)
    {
      // A Dummy Stun Mage
      int iHue = 20 + Team * 40;
      int jHue = 25 + Team * 40;

      // Skills and Stats
      InitStats(90, 90, 125);
      Skills.Magery.Base = 100;
      Skills.EvalInt.Base = 120;
      Skills.Anatomy.Base = 80;
      Skills.Wrestling.Base = 80;
      Skills.Meditation.Base = 100;
      Skills.Poisoning.Base = 100;

      // Equip
      Spellbook book = new Spellbook();
      book.Movable = false;
      book.LootType = LootType.Newbied;
      book.Content = 0xFFFFFFFFFFFFFFFF;
      AddItem(book);

      LeatherArms lea = new LeatherArms();
      lea.Movable = false;
      lea.LootType = LootType.Newbied;
      lea.Crafter = this;
      lea.Quality = ArmorQuality.Regular;
      AddItem(lea);

      LeatherChest lec = new LeatherChest();
      lec.Movable = false;
      lec.LootType = LootType.Newbied;
      lec.Crafter = this;
      lec.Quality = ArmorQuality.Regular;
      AddItem(lec);

      LeatherGorget leg = new LeatherGorget();
      leg.Movable = false;
      leg.LootType = LootType.Newbied;
      leg.Crafter = this;
      leg.Quality = ArmorQuality.Regular;
      AddItem(leg);

      LeatherLegs lel = new LeatherLegs();
      lel.Movable = false;
      lel.LootType = LootType.Newbied;
      lel.Crafter = this;
      lel.Quality = ArmorQuality.Regular;
      AddItem(lel);

      Boots bts = new Boots();
      bts.Hue = iHue;
      AddItem(bts);

      Cap cap = new Cap();
      cap.Hue = iHue;
      AddItem(cap);

      // Spells
      AddSpellAttack(typeof(MagicArrowSpell));
      AddSpellAttack(typeof(WeakenSpell));
      AddSpellAttack(typeof(FireballSpell));
      AddSpellDefense(typeof(WallOfStoneSpell));
      AddSpellDefense(typeof(HealSpell));
    }

    public DummyStun(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "Stun Mage";

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

  public class DummySuper : Dummy
  {
    [Constructible]
    public DummySuper() : base(AIType.AI_Mage, FightMode.Closest, 15, 1, 0.2, 0.6)
    {
      // A Dummy Super Mage
      int iHue = 20 + Team * 40;
      int jHue = 25 + Team * 40;

      // Skills and Stats
      InitStats(125, 125, 125);
      Skills.Magery.Base = 120;
      Skills.EvalInt.Base = 120;
      Skills.Anatomy.Base = 120;
      Skills.Wrestling.Base = 120;
      Skills.Meditation.Base = 120;
      Skills.Poisoning.Base = 100;
      Skills.Inscribe.Base = 100;

      // Equip
      Spellbook book = new Spellbook();
      book.Movable = false;
      book.LootType = LootType.Newbied;
      book.Content = 0xFFFFFFFFFFFFFFFF;
      AddItem(book);

      LeatherArms lea = new LeatherArms();
      lea.Movable = false;
      lea.LootType = LootType.Newbied;
      lea.Crafter = this;
      lea.Quality = ArmorQuality.Regular;
      AddItem(lea);

      LeatherChest lec = new LeatherChest();
      lec.Movable = false;
      lec.LootType = LootType.Newbied;
      lec.Crafter = this;
      lec.Quality = ArmorQuality.Regular;
      AddItem(lec);

      LeatherGorget leg = new LeatherGorget();
      leg.Movable = false;
      leg.LootType = LootType.Newbied;
      leg.Crafter = this;
      leg.Quality = ArmorQuality.Regular;
      AddItem(leg);

      LeatherLegs lel = new LeatherLegs();
      lel.Movable = false;
      lel.LootType = LootType.Newbied;
      lel.Crafter = this;
      lel.Quality = ArmorQuality.Regular;
      AddItem(lel);

      Sandals snd = new Sandals();
      snd.Hue = iHue;
      snd.LootType = LootType.Newbied;
      AddItem(snd);

      JesterHat jhat = new JesterHat();
      jhat.Hue = iHue;
      AddItem(jhat);

      Doublet dblt = new Doublet();
      dblt.Hue = iHue;
      AddItem(dblt);

      // Spells
      AddSpellAttack(typeof(MagicArrowSpell));
      AddSpellAttack(typeof(WeakenSpell));
      AddSpellAttack(typeof(FireballSpell));
      AddSpellDefense(typeof(WallOfStoneSpell));
      AddSpellDefense(typeof(HealSpell));
    }

    public DummySuper(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "Super Mage";

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

  public class DummyHealer : Dummy
  {
    [Constructible]
    public DummyHealer() : base(AIType.AI_Healer, FightMode.Closest, 15, 1, 0.2, 0.6)
    {
      // A Dummy Healer Mage
      int iHue = 20 + Team * 40;
      int jHue = 25 + Team * 40;

      // Skills and Stats
      InitStats(125, 125, 125);
      Skills.Magery.Base = 120;
      Skills.EvalInt.Base = 120;
      Skills.Anatomy.Base = 120;
      Skills.Wrestling.Base = 120;
      Skills.Meditation.Base = 120;
      Skills.Healing.Base = 100;

      // Equip
      Spellbook book = new Spellbook();
      book.Movable = false;
      book.LootType = LootType.Newbied;
      book.Content = 0xFFFFFFFFFFFFFFFF;
      AddItem(book);

      LeatherArms lea = new LeatherArms();
      lea.Movable = false;
      lea.LootType = LootType.Newbied;
      lea.Crafter = this;
      lea.Quality = ArmorQuality.Regular;
      AddItem(lea);

      LeatherChest lec = new LeatherChest();
      lec.Movable = false;
      lec.LootType = LootType.Newbied;
      lec.Crafter = this;
      lec.Quality = ArmorQuality.Regular;
      AddItem(lec);

      LeatherGorget leg = new LeatherGorget();
      leg.Movable = false;
      leg.LootType = LootType.Newbied;
      leg.Crafter = this;
      leg.Quality = ArmorQuality.Regular;
      AddItem(leg);

      LeatherLegs lel = new LeatherLegs();
      lel.Movable = false;
      lel.LootType = LootType.Newbied;
      lel.Crafter = this;
      lel.Quality = ArmorQuality.Regular;
      AddItem(lel);

      Sandals snd = new Sandals();
      snd.Hue = iHue;
      snd.LootType = LootType.Newbied;
      AddItem(snd);

      Cap cap = new Cap();
      cap.Hue = iHue;
      AddItem(cap);

      Robe robe = new Robe();
      robe.Hue = iHue;
      AddItem(robe);
    }

    public DummyHealer(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "Healer";

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

  public class DummyAssassin : Dummy
  {
    [Constructible]
    public DummyAssassin() : base(AIType.AI_Melee, FightMode.Closest, 15, 1, 0.2, 0.6)
    {
      // A Dummy Hybrid Assassin
      int iHue = 20 + Team * 40;
      int jHue = 25 + Team * 40;

      // Skills and Stats
      InitStats(105, 105, 105);
      Skills.Magery.Base = 120;
      Skills.EvalInt.Base = 120;
      Skills.Swords.Base = 120;
      Skills.Tactics.Base = 120;
      Skills.Meditation.Base = 120;
      Skills.Poisoning.Base = 100;

      // Equip
      Spellbook book = new Spellbook();
      book.Movable = false;
      book.LootType = LootType.Newbied;
      book.Content = 0xFFFFFFFFFFFFFFFF;
      AddToBackpack(book);

      Katana kat = new Katana();
      kat.Movable = false;
      kat.LootType = LootType.Newbied;
      kat.Crafter = this;
      kat.Poison = Poison.Deadly;
      kat.PoisonCharges = 12;
      kat.Quality = WeaponQuality.Regular;
      AddToBackpack(kat);

      LeatherArms lea = new LeatherArms();
      lea.Movable = false;
      lea.LootType = LootType.Newbied;
      lea.Crafter = this;
      lea.Quality = ArmorQuality.Regular;
      AddItem(lea);

      LeatherChest lec = new LeatherChest();
      lec.Movable = false;
      lec.LootType = LootType.Newbied;
      lec.Crafter = this;
      lec.Quality = ArmorQuality.Regular;
      AddItem(lec);

      LeatherGorget leg = new LeatherGorget();
      leg.Movable = false;
      leg.LootType = LootType.Newbied;
      leg.Crafter = this;
      leg.Quality = ArmorQuality.Regular;
      AddItem(leg);

      LeatherLegs lel = new LeatherLegs();
      lel.Movable = false;
      lel.LootType = LootType.Newbied;
      lel.Crafter = this;
      lel.Quality = ArmorQuality.Regular;
      AddItem(lel);

      Sandals snd = new Sandals();
      snd.Hue = iHue;
      snd.LootType = LootType.Newbied;
      AddItem(snd);

      Cap cap = new Cap();
      cap.Hue = iHue;
      AddItem(cap);

      Robe robe = new Robe();
      robe.Hue = iHue;
      AddItem(robe);

      DeadlyPoisonPotion pota = new DeadlyPoisonPotion();
      pota.LootType = LootType.Newbied;
      AddToBackpack(pota);

      DeadlyPoisonPotion potb = new DeadlyPoisonPotion();
      potb.LootType = LootType.Newbied;
      AddToBackpack(potb);

      DeadlyPoisonPotion potc = new DeadlyPoisonPotion();
      potc.LootType = LootType.Newbied;
      AddToBackpack(potc);

      DeadlyPoisonPotion potd = new DeadlyPoisonPotion();
      potd.LootType = LootType.Newbied;
      AddToBackpack(potd);

      Bandage band = new Bandage(50);
      AddToBackpack(band);
    }

    public DummyAssassin(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "Hybrid Assassin";

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

  [TypeAlias("Server.Mobiles.DummyTheif")]
  public class DummyThief : Dummy
  {
    [Constructible]
    public DummyThief() : base(AIType.AI_Thief, FightMode.Closest, 15, 1, 0.2, 0.6)
    {
      // A Dummy Hybrid Thief
      int iHue = 20 + Team * 40;
      int jHue = 25 + Team * 40;

      // Skills and Stats
      InitStats(105, 105, 105);
      Skills.Healing.Base = 120;
      Skills.Anatomy.Base = 120;
      Skills.Stealing.Base = 120;
      Skills.ArmsLore.Base = 100;
      Skills.Meditation.Base = 120;
      Skills.Wrestling.Base = 120;

      // Equip
      Spellbook book = new Spellbook();
      book.Movable = false;
      book.LootType = LootType.Newbied;
      book.Content = 0xFFFFFFFFFFFFFFFF;
      AddItem(book);

      LeatherArms lea = new LeatherArms();
      lea.Movable = false;
      lea.LootType = LootType.Newbied;
      lea.Crafter = this;
      lea.Quality = ArmorQuality.Regular;
      AddItem(lea);

      LeatherChest lec = new LeatherChest();
      lec.Movable = false;
      lec.LootType = LootType.Newbied;
      lec.Crafter = this;
      lec.Quality = ArmorQuality.Regular;
      AddItem(lec);

      LeatherGorget leg = new LeatherGorget();
      leg.Movable = false;
      leg.LootType = LootType.Newbied;
      leg.Crafter = this;
      leg.Quality = ArmorQuality.Regular;
      AddItem(leg);

      LeatherLegs lel = new LeatherLegs();
      lel.Movable = false;
      lel.LootType = LootType.Newbied;
      lel.Crafter = this;
      lel.Quality = ArmorQuality.Regular;
      AddItem(lel);

      Sandals snd = new Sandals();
      snd.Hue = iHue;
      snd.LootType = LootType.Newbied;
      AddItem(snd);

      Cap cap = new Cap();
      cap.Hue = iHue;
      AddItem(cap);

      Robe robe = new Robe();
      robe.Hue = iHue;
      AddItem(robe);

      Bandage band = new Bandage(50);
      AddToBackpack(band);
    }

    public DummyThief(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "Hybrid Thief";

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