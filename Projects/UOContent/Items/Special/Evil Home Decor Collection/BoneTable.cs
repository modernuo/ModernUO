namespace Server.Items
{
  public class BoneTableAddon : BaseAddon
  {
    [Constructible]
    public BoneTableAddon()
    {
      AddComponent(new LocalizedAddonComponent(0x2A5C, 1074478), 0, 0, 0);
    }

    public BoneTableAddon(Serial serial) : base(serial)
    {
    }

    public override BaseAddonDeed Deed => new BoneTableDeed();

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

  public class BoneTableDeed : BaseAddonDeed
  {
    [Constructible]
    public BoneTableDeed() => LootType = LootType.Blessed;

    public BoneTableDeed(Serial serial) : base(serial)
    {
    }

    public override BaseAddon Addon => new BoneTableAddon();
    public override int LabelNumber => 1074478; // Bone table

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