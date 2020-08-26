namespace Server.Items
{
  public class MarkScroll : SpellScroll
  {
    [Constructible]
    public MarkScroll(int amount = 1) : base(44, 0x1F59, amount)
    {
    }

    public MarkScroll(Serial serial) : base(serial)
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
