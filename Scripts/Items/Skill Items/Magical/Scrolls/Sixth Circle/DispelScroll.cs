namespace Server.Items
{
  public class DispelScroll : SpellScroll
  {
    [Constructible]
    public DispelScroll() : this(1)
    {
    }

    [Constructible]
    public DispelScroll(int amount) : base(40, 0x1F55, amount)
    {
    }

    public DispelScroll(Serial serial) : base(serial)
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