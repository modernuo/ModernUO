namespace Server.Items
{
  public class DecoTarot7 : Item
  {
    [Constructible]
    public DecoTarot7() : base(0x12A5)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoTarot7(Serial serial) : base(serial)
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