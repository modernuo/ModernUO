namespace Server.Items
{
  public class Bottle : Item, ICommodity
  {
    [Constructible]
    public Bottle(int amount = 1) : base(0xF0E)
    {
      Stackable = true;
      Weight = 1.0;
      Amount = amount;
    }

    public Bottle(Serial serial) : base(serial)
    {
    }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => Core.ML;


    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}
