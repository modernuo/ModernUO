namespace Server.Items
{
  public class DecoRock : Item
  {
    [Constructible]
    public DecoRock() : base(0x1778)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoRock(Serial serial) : base(serial)
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