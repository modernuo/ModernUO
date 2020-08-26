namespace Server.Items
{
  public class DecoTarot6 : Item
  {
    [Constructible]
    public DecoTarot6() : base(0x12AA)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoTarot6(Serial serial) : base(serial)
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