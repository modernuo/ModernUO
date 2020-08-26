namespace Server.Items
{
  public class EnergyBoltScroll : SpellScroll
  {
    [Constructible]
    public EnergyBoltScroll(int amount = 1) : base(41, 0x1F56, amount)
    {
    }

    public EnergyBoltScroll(Serial serial) : base(serial)
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
