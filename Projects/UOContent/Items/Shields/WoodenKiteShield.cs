namespace Server.Items
{
  public class WoodenKiteShield : BaseShield
  {
    [Constructible]
    public WoodenKiteShield() : base(0x1B79) => Weight = 5.0;

    public WoodenKiteShield(Serial serial) : base(serial)
    {
    }

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 0;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 1;

    public override int InitMinHits => 50;
    public override int InitMaxHits => 65;

    public override int AosStrReq => 20;

    public override int ArmorBase => 12;

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      if (Weight == 7.0)
        Weight = 5.0;
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }
  }
}