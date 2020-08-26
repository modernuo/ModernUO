namespace Server.Items
{
  public class Cards : Item
  {
    [Constructible]
    public Cards() : base(0xE19)
    {
      Movable = true;
      Stackable = false;
    }

    public Cards(Serial serial) : base(serial)
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