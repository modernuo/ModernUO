namespace Server.Items
{
  public class Checkers : Item
  {
    [Constructible]
    public Checkers() : base(0xE1A)
    {
      Movable = true;
      Stackable = false;
    }

    public Checkers(Serial serial) : base(serial)
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