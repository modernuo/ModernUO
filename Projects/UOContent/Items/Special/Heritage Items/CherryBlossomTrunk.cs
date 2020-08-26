namespace Server.Items
{
  public class CherryBlossomTrunkAddon : BaseAddon
  {
    [Constructible]
    public CherryBlossomTrunkAddon()
    {
      AddComponent(new LocalizedAddonComponent(0x26EE, 1076784), 0, 0, 0);
    }

    public CherryBlossomTrunkAddon(Serial serial) : base(serial)
    {
    }

    public override BaseAddonDeed Deed => new CherryBlossomTrunkDeed();

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

  public class CherryBlossomTrunkDeed : BaseAddonDeed
  {
    [Constructible]
    public CherryBlossomTrunkDeed() => LootType = LootType.Blessed;

    public CherryBlossomTrunkDeed(Serial serial) : base(serial)
    {
    }

    public override BaseAddon Addon => new CherryBlossomTrunkAddon();
    public override int LabelNumber => 1076784; // Cherry Blossom Trunk

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