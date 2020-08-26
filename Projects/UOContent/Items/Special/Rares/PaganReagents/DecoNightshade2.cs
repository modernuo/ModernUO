namespace Server.Items
{
  public class DecoNightshade2 : Item
  {
    [Constructible]
    public DecoNightshade2() : base(0x18E5)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoNightshade2(Serial serial) : base(serial)
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