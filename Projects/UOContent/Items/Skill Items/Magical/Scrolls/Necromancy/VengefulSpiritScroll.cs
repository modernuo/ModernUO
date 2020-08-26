namespace Server.Items
{
  public class VengefulSpiritScroll : SpellScroll
  {
    [Constructible]
    public VengefulSpiritScroll(int amount = 1) : base(113, 0x226D, amount)
    {
    }

    public VengefulSpiritScroll(Serial serial) : base(serial)
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
