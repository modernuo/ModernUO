namespace Server.Items
{
  public class RangersShortbow : MagicalShortbow
  {
    [Constructible]
    public RangersShortbow() => Attributes.WeaponSpeed = 5;

    public RangersShortbow(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073509; // ranger's shortbow

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