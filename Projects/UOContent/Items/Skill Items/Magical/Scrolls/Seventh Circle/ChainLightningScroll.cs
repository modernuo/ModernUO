namespace Server.Items
{
  public class ChainLightningScroll : SpellScroll
  {
    [Constructible]
    public ChainLightningScroll(int amount = 1) : base(48, 0x1F5D, amount)
    {
    }

    public ChainLightningScroll(Serial serial) : base(serial)
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
