namespace Server.Items
{
  public class NightSightScroll : SpellScroll
  {
    [Constructible]
    public NightSightScroll(int amount = 1) : base(5, 0x1F33, amount)
    {
    }

    public NightSightScroll(Serial ser) : base(ser)
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
