namespace Server.Items
{
  public class DecoNightshade : Item
  {
    [Constructible]
    public DecoNightshade() : base(0x18E7)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoNightshade(Serial serial) : base(serial)
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