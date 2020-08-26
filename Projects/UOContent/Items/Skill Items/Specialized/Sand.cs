namespace Server.Items
{
  [Flippable(0x11EA, 0x11EB)]
  public class Sand : Item, ICommodity
  {
    [Constructible]
    public Sand(int amount = 1) : base(0x11EA)
    {
      Stackable = Core.ML;
      Weight = 1.0;
    }

    public Sand(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1044626; // sand
    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(1); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      if (version == 0 && Name == "sand")
        Name = null;
    }
  }
}
