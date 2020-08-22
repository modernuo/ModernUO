namespace Server.Items
{
  [Flippable]
  public class ParagonChest : LockableContainer
  {
    private static readonly int[] m_ItemIDs =
    {
      0x9AB, 0xE40, 0xE41, 0xE7C
    };

    private static readonly int[] m_Hues =
    {
      0x0, 0x455, 0x47E, 0x89F, 0x8A5, 0x8AB,
      0x966, 0x96D, 0x972, 0x973, 0x979
    };

    private string m_Name;

    [Constructible]
    public ParagonChest(string name, int level) : base(m_ItemIDs.RandomElement())
    {
      m_Name = name;
      Hue = m_Hues.RandomElement();
      Fill(level);
    }

    public ParagonChest(Serial serial) : base(serial)
    {
    }

    public override void OnSingleClick(Mobile from)
    {
      base.OnSingleClick(from);
      LabelTo(from, 1063449, m_Name);
    }

    public override void GetProperties(ObjectPropertyList list)
    {
      base.GetProperties(list);

      list.Add(1063449, m_Name);
    }

    private static void GetRandomAOSStats(out int attributeCount, out int min, out int max)
    {
      int rnd = Utility.Random(15);

      if (rnd < 1)
      {
        attributeCount = Utility.RandomMinMax(2, 6);
        min = 20;
        max = 70;
      }
      else if (rnd < 3)
      {
        attributeCount = Utility.RandomMinMax(2, 4);
        min = 20;
        max = 50;
      }
      else if (rnd < 6)
      {
        attributeCount = Utility.RandomMinMax(2, 3);
        min = 20;
        max = 40;
      }
      else if (rnd < 10)
      {
        attributeCount = Utility.RandomMinMax(1, 2);
        min = 10;
        max = 30;
      }
      else
      {
        attributeCount = 1;
        min = 10;
        max = 20;
      }
    }

    public void Flip()
    {
      ItemID = ItemID switch
      {
        0x9AB => 0xE7C,
        0xE7C => 0x9AB,
        0xE40 => 0xE41,
        0xE41 => 0xE40,
        _ => ItemID
      };
    }

    private void Fill(int level)
    {
      TrapType = TrapType.ExplosionTrap;
      TrapPower = level * 25;
      TrapLevel = level;
      Locked = true;

      RequiredSkill = level switch
      {
        1 => 36,
        2 => 76,
        3 => 84,
        4 => 92,
        5 => 100,
        _ => RequiredSkill
      };

      LockLevel = RequiredSkill - 10;
      MaxLockLevel = RequiredSkill + 40;

      DropItem(new Gold(level * 200));

      for (int i = 0; i < level; ++i)
        DropItem(Loot.RandomScroll(0, 63, SpellbookType.Regular));

      for (int i = 0; i < level * 2; ++i)
      {
        Item item;

        if (Core.AOS)
          item = Loot.RandomArmorOrShieldOrWeaponOrJewelry();
        else
          item = Loot.RandomArmorOrShieldOrWeapon();

        if (item is BaseWeapon weapon)
        {
          if (Core.AOS)
          {
            GetRandomAOSStats(out int attributeCount, out int min, out int max);
            BaseRunicTool.ApplyAttributesTo(weapon, attributeCount, min, max);
          }
          else
          {
            weapon.DamageLevel = (WeaponDamageLevel)Utility.Random(6);
            weapon.AccuracyLevel = (WeaponAccuracyLevel)Utility.Random(6);
            weapon.DurabilityLevel = (WeaponDurabilityLevel)Utility.Random(6);
          }

          DropItem(weapon);
        }
        else if (item is BaseArmor armor)
        {
          if (Core.AOS)
          {
            GetRandomAOSStats(out int attributeCount, out int min, out int max);
            BaseRunicTool.ApplyAttributesTo(armor, attributeCount, min, max);
          }
          else
          {
            armor.ProtectionLevel = (ArmorProtectionLevel)Utility.Random(6);
            armor.Durability = (ArmorDurabilityLevel)Utility.Random(6);
          }

          DropItem(armor);
        }
        else if (item is BaseHat hat)
        {
          if (Core.AOS)
          {
            GetRandomAOSStats(out int attributeCount, out int min, out int max);
            BaseRunicTool.ApplyAttributesTo(hat, attributeCount, min, max);
          }

          DropItem(hat);
        }
        else if (item is BaseJewel jewel)
        {
          GetRandomAOSStats(out int attributeCount, out int min, out int max);
          BaseRunicTool.ApplyAttributesTo(jewel, attributeCount, min, max);

          DropItem(jewel);
        }
      }

      for (int i = 0; i < level; i++)
      {
        Item item = Loot.RandomPossibleReagent();
        item.Amount = Utility.RandomMinMax(40, 60);
        DropItem(item);
      }

      for (int i = 0; i < level; i++)
      {
        Item item = Loot.RandomGem();
        DropItem(item);
      }

      DropItem(new TreasureMap(level + 1, Utility.RandomBool() ? Map.Felucca : Map.Trammel));
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version

      writer.Write(m_Name);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      m_Name = Utility.Intern(reader.ReadString());
    }
  }
}
