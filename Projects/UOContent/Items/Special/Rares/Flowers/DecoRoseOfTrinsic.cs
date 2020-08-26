namespace Server.Items
{
  public class DecoRoseOfTrinsic : Item
  {
    [Constructible]
    public DecoRoseOfTrinsic() : base(0x234C)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoRoseOfTrinsic(Serial serial) : base(serial)
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