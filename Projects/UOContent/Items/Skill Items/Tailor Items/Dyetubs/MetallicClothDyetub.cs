namespace Server.Items
{
  public class MetallicClothDyetub : DyeTub
  {
    [Constructible]
    public MetallicClothDyetub() => LootType = LootType.Blessed;

    public MetallicClothDyetub(Serial serial)
      : base(serial)
    {
    }

    public override int LabelNumber => 1152920; // Metallic Cloth ...

    public override bool MetallicHues => true;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}