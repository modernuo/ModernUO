namespace Server.Items
{
  public class DecoGinseng2 : Item
  {
    [Constructible]
    public DecoGinseng2() : base(0x18EA)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoGinseng2(Serial serial) : base(serial)
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