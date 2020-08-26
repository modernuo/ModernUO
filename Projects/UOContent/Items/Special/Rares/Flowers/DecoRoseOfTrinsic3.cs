namespace Server.Items
{
  public class DecoRoseOfTrinsic3 : Item
  {
    [Constructible]
    public DecoRoseOfTrinsic3() : base(0x234B)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoRoseOfTrinsic3(Serial serial) : base(serial)
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