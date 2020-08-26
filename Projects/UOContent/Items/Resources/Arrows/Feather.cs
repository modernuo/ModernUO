namespace Server.Items
{
  public class Feather : Item, ICommodity
  {
    [Constructible]
    public Feather(int amount = 1) : base(0x1BD1)
    {
      Stackable = true;
      Amount = amount;
    }

    public Feather(Serial serial) : base(serial)
    {
    }

    public override double DefaultWeight => 0.1;
    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;

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
