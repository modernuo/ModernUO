namespace Server.Items
{
  public class WitherScroll : SpellScroll
  {
    [Constructible]
    public WitherScroll(int amount = 1) : base(114, 0x226E, amount)
    {
    }

    public WitherScroll(Serial serial) : base(serial)
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
