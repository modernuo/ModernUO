namespace Server.Items
{
  public class DecoTarot5 : Item
  {
    [Constructible]
    public DecoTarot5() : base(0x12A9)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoTarot5(Serial serial) : base(serial)
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