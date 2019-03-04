namespace Server.Items
{
  public class HealScroll : SpellScroll
  {
    [Constructible]
    public HealScroll(int amount = 1) : base(3, 0x1F31, amount)
    {
    }

    public HealScroll(Serial serial) : base(serial)
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
