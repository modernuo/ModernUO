namespace Server.Items
{
  public class Chessmen : Item
  {
    [Constructible]
    public Chessmen() : base(0xE13)
    {
      Movable = true;
      Stackable = false;
    }

    public Chessmen(Serial serial) : base(serial)
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