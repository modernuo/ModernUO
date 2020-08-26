namespace Server.Items
{
  public class BladeSpiritsScroll : SpellScroll
  {
    [Constructible]
    public BladeSpiritsScroll(int amount = 1) : base(32, 0x1F4D, amount)
    {
    }

    public BladeSpiritsScroll(Serial serial) : base(serial)
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
