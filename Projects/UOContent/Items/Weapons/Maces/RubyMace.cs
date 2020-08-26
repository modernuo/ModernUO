namespace Server.Items
{
  public class RubyMace : DiamondMace
  {
    [Constructible]
    public RubyMace() => Attributes.WeaponDamage = 5;

    public RubyMace(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073529; // ruby mace

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