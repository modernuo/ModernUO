namespace Server.Items
{
  public class DispelScroll : SpellScroll
  {
    [Constructible]
    public DispelScroll(int amount = 1) : base(40, 0x1F55, amount)
    {
    }

    public DispelScroll(Serial serial) : base(serial)
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
