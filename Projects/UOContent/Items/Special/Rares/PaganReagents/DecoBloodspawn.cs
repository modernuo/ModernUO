namespace Server.Items
{
  public class DecoBloodspawn : Item
  {
    [Constructible]
    public DecoBloodspawn() : base(0xF7C)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoBloodspawn(Serial serial) : base(serial)
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