using System;

namespace Server.Items
{
  public class TreasureChestLevel4 : LockableContainer
  {
    private const int m_Level = 4;

    [Constructible]
    public TreasureChestLevel4()
      : base(0xE41)
    {
      SetChestAppearance();
      Movable = false;

      TrapType = TrapType.ExplosionTrap;
      TrapPower = m_Level * Utility.Random(10, 25);
      Locked = true;

      RequiredSkill = 92;
      LockLevel = RequiredSkill - Utility.Random(1, 10);
      MaxLockLevel = RequiredSkill + Utility.Random(1, 10);

      // According to OSI, loot in level 4 chest is:
      //  Gold 500 - 900
      //  Reagents
      //  Scrolls
      //  Blank scrolls
      //  Potions
      //  Gems
      //  Magic Wand
      //  Magic weapon
      //  Magic armour
      //  Magic clothing (not implemented)
      //  Magic jewelry (not implemented)
      //  Crystal ball (not implemented)

      // Gold
      DropItem(new Gold(Utility.Random(200, 400)));

      // Reagents
      for (int i = Utility.Random(1, m_Level); i > 1; i--)
      {
        Item ReagentLoot = Loot.RandomReagent();
        ReagentLoot.Amount = 12;
        DropItem(ReagentLoot);
      }

      // Scrolls
      for (int i = Utility.Random(1, m_Level); i > 1; i--)
      {
        Item ScrollLoot = Loot.RandomScroll(0, 47, SpellbookType.Regular);
        ScrollLoot.Amount = 16;
        DropItem(ScrollLoot);
      }

      // Drop blank scrolls
      DropItem(new BlankScroll(Utility.Random(1, m_Level)));

      // Potions
      for (int i = Utility.Random(1, m_Level); i > 1; i--)
      {
        Item PotionLoot = Loot.RandomPotion();
        DropItem(PotionLoot);
      }

      // Gems
      for (int i = Utility.Random(1, m_Level); i > 1; i--)
      {
        Item GemLoot = Loot.RandomGem();
        GemLoot.Amount = 12;
        DropItem(GemLoot);
      }

      // Magic Wand
      for (int i = Utility.Random(1, m_Level); i > 1; i--)
        DropItem(Loot.RandomWand());

      // Equipment
      for (int i = Utility.Random(1, m_Level); i > 1; i--)
      {
        Item item = Loot.RandomArmorOrShieldOrWeapon();

        if (item is BaseWeapon weapon)
        {
          weapon.DamageLevel = (WeaponDamageLevel)Utility.Random(m_Level);
          weapon.AccuracyLevel = (WeaponAccuracyLevel)Utility.Random(m_Level);
          weapon.DurabilityLevel = (WeaponDurabilityLevel)Utility.Random(m_Level);
          weapon.Quality = WeaponQuality.Regular;
        }
        else if (item is BaseArmor armor)
        {
          armor.ProtectionLevel = (ArmorProtectionLevel)Utility.Random(m_Level);
          armor.Durability = (ArmorDurabilityLevel)Utility.Random(m_Level);
          armor.Quality = ArmorQuality.Regular;
        }

        DropItem(item);
      }

      // Clothing
      for (int i = Utility.Random(1, 2); i > 1; i--)
        DropItem(Loot.RandomClothing());

      // Jewelry
      for (int i = Utility.Random(1, 2); i > 1; i--)
        DropItem(Loot.RandomJewelry());

      // Crystal ball (not implemented)
    }

    public TreasureChestLevel4(Serial serial)
      : base(serial)
    {
    }

    public override bool Decays => true;

    public override bool IsDecoContainer => false;

    public override TimeSpan DecayTime => TimeSpan.FromMinutes(Utility.Random(15, 60));

    public override int DefaultGumpID => 0x42;

    public override int DefaultDropSound => 0x42;

    public override Rectangle2D Bounds => new Rectangle2D(18, 105, 144, 73);

    private void SetChestAppearance()
    {
      bool UseFirstItemId = Utility.RandomBool();

      switch (Utility.Random(4))
      {
        case 0: // Wooden Chest
          ItemID = UseFirstItemId ? 0xe42 : 0xe43;
          GumpID = 0x49;
          break;

        case 1: // Metal Chest
          ItemID = UseFirstItemId ? 0x9ab : 0xe7c;
          GumpID = 0x4A;
          break;

        case 2: // Metal Golden Chest
          ItemID = UseFirstItemId ? 0xe40 : 0xe41;
          GumpID = 0x42;
          break;

        case 3: // Keg
          ItemID = 0xe7f;
          GumpID = 0x3e;
          break;
      }
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);
      writer.Write(1); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);
      int version = reader.ReadInt();
    }
  }
}
