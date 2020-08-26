namespace Server.Items
{
  public class AlchemyStone : Item
  {
    [Constructible]
    public AlchemyStone() : base(0xED4)
    {
      Movable = false;
      Hue = 0x250;
    }

    public AlchemyStone(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "an Alchemist Supply Stone";

    public override void OnDoubleClick(Mobile from)
    {
      AlchemyBag alcBag = new AlchemyBag();

      if (!from.AddToBackpack(alcBag))
        alcBag.Delete();
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