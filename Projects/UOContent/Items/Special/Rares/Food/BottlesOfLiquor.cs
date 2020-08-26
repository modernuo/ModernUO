namespace Server.Items
{
  public class DecoBottlesOfLiquor : Item
  {
    [Constructible]
    public DecoBottlesOfLiquor() : base(0x99E)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoBottlesOfLiquor(Serial serial) : base(serial)
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