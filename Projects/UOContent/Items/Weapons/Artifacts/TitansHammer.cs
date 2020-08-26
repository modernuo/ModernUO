namespace Server.Items
{
  public class TitansHammer : WarHammer
  {
    [Constructible]
    public TitansHammer()
    {
      Hue = 0x482;
      WeaponAttributes.HitEnergyArea = 100;
      Attributes.BonusStr = 15;
      Attributes.AttackChance = 15;
      Attributes.WeaponDamage = 50;
    }

    public TitansHammer(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1060024; // Titan's Hammer
    public override int ArtifactRarity => 10;

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