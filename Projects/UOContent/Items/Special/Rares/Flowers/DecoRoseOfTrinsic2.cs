namespace Server.Items
{
  public class DecoRoseOfTrinsic2 : Item
  {
    [Constructible]
    public DecoRoseOfTrinsic2() : base(0x234D)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoRoseOfTrinsic2(Serial serial) : base(serial)
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