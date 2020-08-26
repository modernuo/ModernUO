namespace Server.Items
{
  public class MagicUnTrapScroll : SpellScroll
  {
    [Constructible]
    public MagicUnTrapScroll(int amount = 1) : base(13, 0x1F3A, amount)
    {
    }

    public MagicUnTrapScroll(Serial serial) : base(serial)
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
