namespace Server.Items
{
  public class Cards2 : Item
  {
    [Constructible]
    public Cards2() : base(0xE16)
    {
      Movable = true;
      Stackable = false;
    }

    public Cards2(Serial serial) : base(serial)
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