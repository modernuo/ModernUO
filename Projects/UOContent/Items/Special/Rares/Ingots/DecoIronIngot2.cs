namespace Server.Items
{
  public class DecoIronIngot2 : Item
  {
    [Constructible]
    public DecoIronIngot2() : base(0x1BEF)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoIronIngot2(Serial serial) : base(serial)
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