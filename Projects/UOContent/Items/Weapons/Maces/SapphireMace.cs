namespace Server.Items
{
  public class SapphireMace : DiamondMace
  {
    [Constructible]
    public SapphireMace() => WeaponAttributes.ResistEnergyBonus = 5;

    public SapphireMace(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073531; // sapphire mace

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