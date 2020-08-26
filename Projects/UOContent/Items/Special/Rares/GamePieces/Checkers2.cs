namespace Server.Items
{
  public class Checkers2 : Item
  {
    [Constructible]
    public Checkers2() : base(0xE1B)
    {
      Movable = true;
      Stackable = false;
    }

    public Checkers2(Serial serial) : base(serial)
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