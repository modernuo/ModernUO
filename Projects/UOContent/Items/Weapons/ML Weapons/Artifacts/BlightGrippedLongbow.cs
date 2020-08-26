namespace Server.Items
{
  public class BlightGrippedLongbow : ElvenCompositeLongbow
  {
    [Constructible]
    public BlightGrippedLongbow()
    {
      Hue = 0x8A4;

      WeaponAttributes.HitPoisonArea = 20;
      Attributes.RegenStam = 3;
      Attributes.NightSight = 1;
      Attributes.WeaponSpeed = 20;
      Attributes.WeaponDamage = 35;
    }

    public BlightGrippedLongbow(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1072907; // Blight Gripped Longbow

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