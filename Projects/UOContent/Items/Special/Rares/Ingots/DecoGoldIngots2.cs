namespace Server.Items
{
  public class DecoGoldIngots2 : Item
  {
    [Constructible]
    public DecoGoldIngots2() : base(0x1BEB)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoGoldIngots2(Serial serial) : base(serial)
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