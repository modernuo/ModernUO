namespace Server.Items
{
  public class PoisonStrikeScroll : SpellScroll
  {
    [Constructible]
    public PoisonStrikeScroll(int amount = 1) : base(109, 0x2269, amount)
    {
    }

    public PoisonStrikeScroll(Serial serial) : base(serial)
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
