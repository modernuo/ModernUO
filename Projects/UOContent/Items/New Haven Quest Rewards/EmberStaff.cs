namespace Server.Items
{
  public class EmberStaff : QuarterStaff
  {
    [Constructible]
    public EmberStaff()
    {
      LootType = LootType.Blessed;

      WeaponAttributes.HitFireball = 15;
      WeaponAttributes.MageWeapon = 10;
      Attributes.SpellChanneling = 1;
      Attributes.CastSpeed = -1;
      WeaponAttributes.LowerStatReq = 50;
    }

    public EmberStaff(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1077582; // Ember Staff

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