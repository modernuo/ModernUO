namespace Server.Items
{
  public class Buckler : BaseShield
  {
    [Constructible]
    public Buckler() : base(0x1B73) => Weight = 5.0;

    public Buckler(Serial serial) : base(serial)
    {
    }

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 0;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 1;
    public override int BaseEnergyResistance => 0;

    public override int InitMinHits => 40;
    public override int InitMaxHits => 50;

    public override int AosStrReq => 20;

    public override int ArmorBase => 7;

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }
  }
}