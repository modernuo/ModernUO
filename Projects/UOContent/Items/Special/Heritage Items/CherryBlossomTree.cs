namespace Server.Items
{
  public class CherryBlossomTreeAddon : BaseAddon
  {
    [Constructible]
    public CherryBlossomTreeAddon()
    {
      AddComponent(new LocalizedAddonComponent(0x26EE, 1076268), 0, 0, 0);
      AddComponent(new LocalizedAddonComponent(0x3122, 1076268), 0, 0, 0);
    }

    public CherryBlossomTreeAddon(Serial serial) : base(serial)
    {
    }

    public override BaseAddonDeed Deed => new CherryBlossomTreeDeed();

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

  public class CherryBlossomTreeDeed : BaseAddonDeed
  {
    [Constructible]
    public CherryBlossomTreeDeed() => LootType = LootType.Blessed;

    public CherryBlossomTreeDeed(Serial serial) : base(serial)
    {
    }

    public override BaseAddon Addon => new CherryBlossomTreeAddon();
    public override int LabelNumber => 1076268; // Cherry Blossom Tree

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