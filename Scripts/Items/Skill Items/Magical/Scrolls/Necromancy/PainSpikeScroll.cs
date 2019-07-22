namespace Server.Items
{
  public class PainSpikeScroll : SpellScroll
  {
    [Constructible]
    public PainSpikeScroll(int amount = 1) : base(108, 0x2268, amount)
    {
    }

    public PainSpikeScroll(Serial serial) : base(serial)
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
