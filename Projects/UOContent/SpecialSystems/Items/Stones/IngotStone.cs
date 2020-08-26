namespace Server.Items
{
  public class IngotStone : Item
  {
    [Constructible]
    public IngotStone() : base(0xED4)
    {
      Movable = false;
      Hue = 0x480;
    }

    public IngotStone(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "an Ingot stone";

    public override void OnDoubleClick(Mobile from)
    {
      BagOfingots ingotBag = new BagOfingots();

      if (!from.AddToBackpack(ingotBag))
        ingotBag.Delete();
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