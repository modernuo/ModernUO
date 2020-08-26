namespace Server.Items
{
  public class DecoHorseDung : Item
  {
    [Constructible]
    public DecoHorseDung() : base(0xF3B)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoHorseDung(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}