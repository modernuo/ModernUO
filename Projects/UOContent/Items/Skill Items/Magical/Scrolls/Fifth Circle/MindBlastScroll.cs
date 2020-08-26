namespace Server.Items
{
  public class MindBlastScroll : SpellScroll
  {
    [Constructible]
    public MindBlastScroll(int amount = 1) : base(36, 0x1F51, amount)
    {
    }

    public MindBlastScroll(Serial serial) : base(serial)
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
