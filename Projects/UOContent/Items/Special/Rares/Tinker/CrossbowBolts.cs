namespace Server.Items
{
  public class CrossbowBolts : Item
  {
    [Constructible]
    public CrossbowBolts() : base(0x1BFC)
    {
      Movable = true;
      Stackable = false;
    }

    public CrossbowBolts(Serial serial) : base(serial)
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