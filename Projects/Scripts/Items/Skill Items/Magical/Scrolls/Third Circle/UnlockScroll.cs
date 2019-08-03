namespace Server.Items
{
  public class UnlockScroll : SpellScroll
  {
    [Constructible]
    public UnlockScroll(int amount = 1) : base(22, 0x1F43, amount)
    {
    }

    public UnlockScroll(Serial serial) : base(serial)
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
