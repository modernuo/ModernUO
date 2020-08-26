namespace Server.Items
{
  public class DecoHay : Item
  {
    [Constructible]
    public DecoHay() : base(0xF35)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoHay(Serial serial) : base(serial)
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