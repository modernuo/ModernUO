namespace Server.Items
{
  public class MindRotScroll : SpellScroll
  {
    [Constructible]
    public MindRotScroll(int amount = 1) : base(107, 0x2267, amount)
    {
    }

    public MindRotScroll(Serial serial) : base(serial)
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
