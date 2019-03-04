namespace Server.Items
{
  public class SummonFireElementalScroll : SpellScroll
  {
    [Constructible]
    public SummonFireElementalScroll(int amount = 1) : base(62, 0x1F6B, amount)
    {
    }

    public SummonFireElementalScroll(Serial serial) : base(serial)
    {
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}
