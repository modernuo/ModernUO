namespace Server.Items
{
  public class DecoMandrake2 : Item
  {
    [Constructible]
    public DecoMandrake2() : base(0x18E0)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoMandrake2(Serial serial) : base(serial)
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