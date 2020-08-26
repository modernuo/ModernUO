namespace Server.Items
{
  public class BloodOathScroll : SpellScroll
  {
    [Constructible]
    public BloodOathScroll(int amount = 1) : base(101, 0x2261, amount)
    {
    }

    public BloodOathScroll(Serial serial) : base(serial)
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
