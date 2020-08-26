namespace Server.Items
{
  public class BlankScroll : Item, ICommodity
  {
    [Constructible]
    public BlankScroll(int amount = 1) : base(0xEF3)
    {
      Stackable = true;
      Weight = 1.0;
      Amount = amount;
    }

    public BlankScroll(Serial serial) : base(serial)
    {
    }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => Core.ML;

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
