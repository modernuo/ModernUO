namespace Server.Items
{
  public class DecoBlackmoor : Item
  {
    [Constructible]
    public DecoBlackmoor() : base(0xF79)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoBlackmoor(Serial serial) : base(serial)
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