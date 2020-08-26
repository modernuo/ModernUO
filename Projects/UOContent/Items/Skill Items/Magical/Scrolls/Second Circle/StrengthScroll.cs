namespace Server.Items
{
  public class StrengthScroll : SpellScroll
  {
    [Constructible]
    public StrengthScroll(int amount = 1) : base(15, 0x1F3C, amount)
    {
    }

    public StrengthScroll(Serial serial) : base(serial)
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
