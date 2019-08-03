namespace Server.Items
{
  public class TelekinisisScroll : SpellScroll
  {
    [Constructible]
    public TelekinisisScroll(int amount = 1) : base(20, 0x1F41, amount)
    {
    }

    public TelekinisisScroll(Serial serial) : base(serial)
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
