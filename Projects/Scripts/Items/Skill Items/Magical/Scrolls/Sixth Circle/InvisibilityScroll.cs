namespace Server.Items
{
  public class InvisibilityScroll : SpellScroll
  {
    [Constructible]
    public InvisibilityScroll(int amount = 1) : base(43, 0x1F58, amount)
    {
    }

    public InvisibilityScroll(Serial serial) : base(serial)
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
