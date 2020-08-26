using System;

namespace Server.Items
{
  public class TreasureChestLevel1 : LockableContainer
  {
    private const int m_Level = 1;

    [Constructible]
    public TreasureChestLevel1()
      : base(0xE41)
    {
      SetChestAppearance();
      Movable = false;

      TrapType = TrapType.DartTrap;
      TrapPower = m_Level * Utility.Random(1, 25);
      Locked = true;

      RequiredSkill = 57;
      LockLevel = RequiredSkill - Utility.Random(1, 10);
      MaxLockLevel = RequiredSkill + Utility.Random(1, 10);

      // According to OSI, loot in level 1 chest is:
      //  Gold 25 - 50
      //  Bolts 10
      //  Gems
      //  Normal weapon
      //  Normal armour
      //  Normal clothing
      //  Normal jewelry

      // Gold
      DropItem(new Gold(Utility.Random(30, 100)));

      // Drop bolts
      // DropItem( new Bolt( 10 ) );

      // Gems
      if (Utility.RandomBool())
      {
        Item GemLoot = Loot.RandomGem();
        GemLoot.Amount = Utility.Random(1, 3);
        DropItem(GemLoot);
      }

      // Weapon
      if (Utility.RandomBool())
        DropItem(Loot.RandomWeapon());

      // Armour
      if (Utility.RandomBool())
        DropItem(Loot.RandomArmorOrShield());

      // Clothing
      if (Utility.RandomBool())
        DropItem(Loot.RandomClothing());

      // Jewelry
      if (Utility.RandomBool())
        DropItem(Loot.RandomJewelry());
    }

    public TreasureChestLevel1(Serial serial)
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

      switch (Utility.RandomList(0, 1, 2))
      {
        case 0: // Large Crate
          ItemID = UseFirstItemId ? 0xe3c : 0xe3d;
          GumpID = 0x44;
          break;

        case 1: // Medium Crate
          ItemID = UseFirstItemId ? 0xe3e : 0xe3f;
          GumpID = 0x44;
          break;

        case 2: // Small Crate
          ItemID = UseFirstItemId ? 0x9a9 : 0xe7e;
          GumpID = 0x44;
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