namespace Server.Items
{
  public class DecoSilverIngot : Item
  {
    [Constructible]
    public DecoSilverIngot() : base(0x1BF5)
    {
      Movable = true;
      Stackable = true;
    }

    public DecoSilverIngot(Serial serial) : base(serial)
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