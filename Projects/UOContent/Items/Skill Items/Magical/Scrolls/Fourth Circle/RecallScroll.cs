namespace Server.Items
{
  public class RecallScroll : SpellScroll
  {
    [Constructible]
    public RecallScroll(int amount = 1) : base(31, 0x1F4C, amount)
    {
    }

    public RecallScroll(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}
