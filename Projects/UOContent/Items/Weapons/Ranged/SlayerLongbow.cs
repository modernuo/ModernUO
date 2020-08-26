namespace Server.Items
{
  public class SlayerLongbow : ElvenCompositeLongbow
  {
    [Constructible]
    public SlayerLongbow() => Slayer2 = (SlayerName)Utility.RandomMinMax(1, 27);

    public SlayerLongbow(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1073506; // slayer longbow

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