namespace Server.Items
{
  public class Cards4 : Item
  {
    [Constructible]
    public Cards4() : base(0xE17)
    {
      Movable = true;
      Stackable = false;
    }

    public Cards4(Serial serial) : base(serial)
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