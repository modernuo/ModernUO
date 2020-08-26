namespace Server.Items
{
  public class DecoBridle : Item
  {
    [Constructible]
    public DecoBridle() : base(0x1374)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoBridle(Serial serial) : base(serial)
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