namespace Server.Items
{
  [Flippable(0xC12, 0xC13)]
  public class BrokenArmoireComponent : AddonComponent
  {
    public BrokenArmoireComponent() : base(0xC12)
    {
    }

    public BrokenArmoireComponent(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1076262; // Broken Armoire

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

  public class BrokenArmoireAddon : BaseAddon
  {
    [Constructible]
    public BrokenArmoireAddon()
    {
      AddComponent(new BrokenArmoireComponent(), 0, 0, 0);
    }

    public BrokenArmoireAddon(Serial serial) : base(serial)
    {
    }

    public override BaseAddonDeed Deed => new BrokenArmoireDeed();

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

  public class BrokenArmoireDeed : BaseAddonDeed
  {
    [Constructible]
    public BrokenArmoireDeed() => LootType = LootType.Blessed;

    public BrokenArmoireDeed(Serial serial) : base(serial)
    {
    }

    public override BaseAddon Addon => new BrokenArmoireAddon();
    public override int LabelNumber => 1076262; // Broken Armoire

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