namespace Server.Items
{
  public class CurseScroll : SpellScroll
  {
    [Constructible]
    public CurseScroll(int amount = 1) : base(26, 0x1F47, amount)
    {
    }

    public CurseScroll(Serial serial) : base(serial)
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
