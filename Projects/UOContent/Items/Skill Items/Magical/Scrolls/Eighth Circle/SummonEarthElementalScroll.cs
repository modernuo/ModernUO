namespace Server.Items
{
  public class SummonEarthElementalScroll : SpellScroll
  {
    [Constructible]
    public SummonEarthElementalScroll(int amount = 1) : base(61, 0x1F6A, amount)
    {
    }

    public SummonEarthElementalScroll(Serial serial) : base(serial)
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
