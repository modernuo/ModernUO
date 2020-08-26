namespace Server.Items
{
  [Flippable(0x2FB7, 0x3171)]
  public class ElvenQuiver : BaseQuiver
  {
    [Constructible]
    public ElvenQuiver() => WeightReduction = 30;

    public ElvenQuiver(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1032657; // elven quiver

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