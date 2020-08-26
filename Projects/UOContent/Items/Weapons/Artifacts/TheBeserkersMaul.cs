namespace Server.Items
{
  public class TheBeserkersMaul : Maul
  {
    [Constructible]
    public TheBeserkersMaul()
    {
      Hue = 0x21;
      Attributes.WeaponSpeed = 75;
      Attributes.WeaponDamage = 50;
    }

    public TheBeserkersMaul(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1061108; // The Berserker's Maul
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