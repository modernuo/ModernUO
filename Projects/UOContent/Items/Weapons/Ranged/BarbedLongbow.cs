namespace Server.Items
{
  public class BarbedLongbow : ElvenCompositeLongbow
  {
    [Constructible]
    public BarbedLongbow() => Attributes.ReflectPhysical = 12;

    public BarbedLongbow(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073505; // barbed longbow

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