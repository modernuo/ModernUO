namespace Server.Items
{
  public class DecoEyeOfNewt : Item
  {
    [Constructible]
    public DecoEyeOfNewt() : base(0xF87)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoEyeOfNewt(Serial serial) : base(serial)
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