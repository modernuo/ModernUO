namespace Server.Items
{
  public class AxeOfTheHeavens : DoubleAxe
  {
    [Constructible]
    public AxeOfTheHeavens()
    {
      Hue = 0x4D5;
      WeaponAttributes.HitLightning = 50;
      Attributes.AttackChance = 15;
      Attributes.DefendChance = 15;
      Attributes.WeaponDamage = 50;
    }

    public AxeOfTheHeavens(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1061106; // Axe of the Heavens
    public override int ArtifactRarity => 11;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}