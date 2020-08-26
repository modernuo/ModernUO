namespace Server.Items
{
  public class DispelFieldScroll : SpellScroll
  {
    [Constructible]
    public DispelFieldScroll(int amount = 1) : base(33, 0x1F4E, amount)
    {
    }

    public DispelFieldScroll(Serial serial) : base(serial)
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
