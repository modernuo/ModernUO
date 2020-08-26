namespace Server.Items
{
  public class DecoSilverIngots3 : Item
  {
    [Constructible]
    public DecoSilverIngots3() : base(0x1BF7)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoSilverIngots3(Serial serial) : base(serial)
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