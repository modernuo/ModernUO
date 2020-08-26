namespace Server.Items
{
  [Flippable(0x3D86, 0x3D87)]
  public class SuitOfSilverArmorComponent : AddonComponent
  {
    public SuitOfSilverArmorComponent() : base(0x3D86)
    {
    }

    public SuitOfSilverArmorComponent(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1076266; // Suit of Silver Armor

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }

  public class SuitOfSilverArmorAddon : BaseAddon
  {
    [Constructible]
    public SuitOfSilverArmorAddon()
    {
      AddComponent(new SuitOfSilverArmorComponent(), 0, 0, 0);
    }

    public SuitOfSilverArmorAddon(Serial serial) : base(serial)
    {
    }

    public override BaseAddonDeed Deed => new SuitOfSilverArmorDeed();

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }

  public class SuitOfSilverArmorDeed : BaseAddonDeed
  {
    [Constructible]
    public SuitOfSilverArmorDeed() => LootType = LootType.Blessed;

    public SuitOfSilverArmorDeed(Serial serial) : base(serial)
    {
    }

    public override BaseAddon Addon => new SuitOfSilverArmorAddon();
    public override int LabelNumber => 1076266; // Suit of Silver Armor

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }
}