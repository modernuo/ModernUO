namespace Server.Items
{
  public class DecoGoldIngots : Item
  {
    [Constructible]
    public DecoGoldIngots() : base(0x1BEA)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoGoldIngots(Serial serial) : base(serial)
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