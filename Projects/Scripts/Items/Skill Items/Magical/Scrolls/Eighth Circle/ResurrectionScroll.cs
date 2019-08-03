namespace Server.Items
{
  public class ResurrectionScroll : SpellScroll
  {
    [Constructible]
    public ResurrectionScroll(int amount = 1) : base(58, 0x1F67, amount)
    {
    }

    public ResurrectionScroll(Serial serial) : base(serial)
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
