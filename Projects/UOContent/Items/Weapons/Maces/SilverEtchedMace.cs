namespace Server.Items
{
  public class SilverEtchedMace : DiamondMace
  {
    [Constructible]
    public SilverEtchedMace() => Slayer = SlayerName.Exorcism;

    public SilverEtchedMace(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073532; // silver-etched mace

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }
}