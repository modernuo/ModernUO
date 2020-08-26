namespace Server.Items
{
  public class DecoSpittoon : Item
  {
    [Constructible]
    public DecoSpittoon() : base(0x1003)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoSpittoon(Serial serial) : base(serial)
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