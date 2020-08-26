namespace Server.Items
{
  public class EmptyToolKit : Item
  {
    [Constructible]
    public EmptyToolKit() : base(0x1EB6)
    {
      Movable = true;
      Stackable = false;
    }

    public EmptyToolKit(Serial serial) : base(serial)
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