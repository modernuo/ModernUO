namespace Server.Items
{
  public class ThornedWildStaff : WildStaff
  {
    [Constructible]
    public ThornedWildStaff() => Attributes.ReflectPhysical = 12;

    public ThornedWildStaff(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073551; // thorned wild staff

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