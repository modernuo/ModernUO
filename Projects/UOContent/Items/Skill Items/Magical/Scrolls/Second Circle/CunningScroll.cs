namespace Server.Items
{
  public class CunningScroll : SpellScroll
  {
    [Constructible]
    public CunningScroll(int amount = 1) : base(9, 0x1F36, amount)
    {
    }

    public CunningScroll(Serial serial) : base(serial)
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
