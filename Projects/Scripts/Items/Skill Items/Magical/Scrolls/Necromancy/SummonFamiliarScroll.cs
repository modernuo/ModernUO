namespace Server.Items
{
  public class SummonFamiliarScroll : SpellScroll
  {
    [Constructible]
    public SummonFamiliarScroll(int amount = 1) : base(111, 0x226B, amount)
    {
    }

    public SummonFamiliarScroll(Serial serial) : base(serial)
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
