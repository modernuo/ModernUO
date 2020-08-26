namespace Server.Items
{
  public class MetalKiteShield : BaseShield, IDyable
  {
    [Constructible]
    public MetalKiteShield() : base(0x1B74) => Weight = 7.0;

    public MetalKiteShield(Serial serial) : base(serial)
    {
    }

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 0;
    public override int BaseColdResistance => 0;
    public override int BasePoisonResistance => 0;
    public override int BaseEnergyResistance => 1;

    public override int InitMinHits => 45;
    public override int InitMaxHits => 60;

    public override int AosStrReq => 45;

    public override int ArmorBase => 16;

    public bool Dye(Mobile from, DyeTub sender)
    {
      if (Deleted)
        return false;

      Hue = sender.DyedHue;

      return true;
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      if (Weight == 5.0)
        Weight = 7.0;
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }
  }
}