namespace Server.Items
{
  public class PeachTrunkAddon : BaseAddon
  {
    [Constructible]
    public PeachTrunkAddon()
    {
      AddComponent(new LocalizedAddonComponent(0xD9C, 1076786), 0, 0, 0);
    }

    public PeachTrunkAddon(Serial serial) : base(serial)
    {
    }

    public override BaseAddonDeed Deed => new PeachTrunkDeed();

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

  public class PeachTrunkDeed : BaseAddonDeed
  {
    [Constructible]
    public PeachTrunkDeed() => LootType = LootType.Blessed;

    public PeachTrunkDeed(Serial serial) : base(serial)
    {
    }

    public override BaseAddon Addon => new PeachTrunkAddon();
    public override int LabelNumber => 1076786; // Peach Trunk

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