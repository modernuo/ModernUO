namespace Server.Items
{
  public class ExplosionScroll : SpellScroll
  {
    [Constructible]
    public ExplosionScroll(int amount = 1) : base(42, 0x1F57, amount)
    {
    }

    public ExplosionScroll(Serial serial) : base(serial)
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
