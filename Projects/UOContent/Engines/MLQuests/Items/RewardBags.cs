using Server.Items;

namespace Server.Engines.MLQuests.Items
{
  public static class RewardBag
  {
    public static void Fill(Container c, int itemCount, double talismanChance)
    {
      c.Hue = Utility.RandomNondyedHue();

      int done = 0;

      if (Utility.RandomDouble() < talismanChance)
      {
        c.DropItem(new RandomTalisman());
        ++done;
      }

      for (; done < itemCount; ++done)
      {
        var loot = Utility.Random(5) switch
        {
          0 => (Item)Loot.RandomWeapon(false, true),
          1 => Loot.RandomArmor(false, true),
          2 => Loot.RandomRangedWeapon(false, true),
          3 => Loot.RandomJewelry(),
          _ => Loot.RandomHat(false) // 4
        };

        if (loot == null)
          continue;

        Enhance(loot);
        c.DropItem(loot);
      }
    }

    public static void Enhance(Item loot)
    {
      if (loot is BaseWeapon weapon)
      {
        BaseRunicTool.ApplyAttributesTo(weapon, Utility.RandomMinMax(1, 5), 10, 80);
        return;
      }

      if (loot is BaseArmor armor) BaseRunicTool.ApplyAttributesTo(armor, Utility.RandomMinMax(1, 5), 10, 80);

      if (loot is BaseJewel jewel) BaseRunicTool.ApplyAttributesTo(jewel, Utility.RandomMinMax(1, 5), 10, 80);
    }
  }

  public class SmallBagOfTrinkets : Bag
  {
    [Constructible]
    public SmallBagOfTrinkets()
    {
      RewardBag.Fill(this, 1, 0.0);
    }

    public SmallBagOfTrinkets(Serial serial)
      : base(serial)
    {
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

  public class BagOfTrinkets : Bag
  {
    [Constructible]
    public BagOfTrinkets()
    {
      RewardBag.Fill(this, 2, 0.05);
    }

    public BagOfTrinkets(Serial serial)
      : base(serial)
    {
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

  public class BagOfTreasure : Bag
  {
    [Constructible]
    public BagOfTreasure()
    {
      RewardBag.Fill(this, 3, 0.20);
    }

    public BagOfTreasure(Serial serial)
      : base(serial)
    {
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

  public class LargeBagOfTreasure : Bag
  {
    [Constructible]
    public LargeBagOfTreasure()
    {
      RewardBag.Fill(this, 4, 0.50);
    }

    public LargeBagOfTreasure(Serial serial)
      : base(serial)
    {
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

  public class RewardStrongbox : WoodenBox
  {
    [Constructible]
    public RewardStrongbox()
    {
      RewardBag.Fill(this, 5, 1.0);
    }

    public RewardStrongbox(Serial serial)
      : base(serial)
    {
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
