namespace Server.Items
{
  [Flippable(0xC24, 0xC25)]
  public class BrokenChestOfDrawersComponent : AddonComponent
  {
    public BrokenChestOfDrawersComponent() : base(0xC24)
    {
    }

    public BrokenChestOfDrawersComponent(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1076261; // Broken Chest of Drawers

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

  public class BrokenChestOfDrawersAddon : BaseAddon
  {
    [Constructible]
    public BrokenChestOfDrawersAddon()
    {
      AddComponent(new BrokenChestOfDrawersComponent(), 0, 0, 0);
    }

    public BrokenChestOfDrawersAddon(Serial serial) : base(serial)
    {
    }

    public override BaseAddonDeed Deed => new BrokenChestOfDrawersDeed();

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

  public class BrokenChestOfDrawersDeed : BaseAddonDeed
  {
    [Constructible]
    public BrokenChestOfDrawersDeed() => LootType = LootType.Blessed;

    public BrokenChestOfDrawersDeed(Serial serial) : base(serial)
    {
    }

    public override BaseAddon Addon => new BrokenChestOfDrawersAddon();
    public override int LabelNumber => 1076261; // Broken Chest of Drawers

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