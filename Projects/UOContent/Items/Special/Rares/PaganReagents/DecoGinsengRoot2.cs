namespace Server.Items
{
  public class DecoGinsengRoot2 : Item
  {
    [Constructible]
    public DecoGinsengRoot2() : base(0x18EC)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoGinsengRoot2(Serial serial) : base(serial)
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