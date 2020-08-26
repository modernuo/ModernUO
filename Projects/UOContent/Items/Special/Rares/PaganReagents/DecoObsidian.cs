namespace Server.Items
{
  public class DecoObsidian : Item
  {
    [Constructible]
    public DecoObsidian() : base(0xF89)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoObsidian(Serial serial) : base(serial)
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