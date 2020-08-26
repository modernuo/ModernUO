namespace Server.Items
{
  public class Garlic : BaseReagent, ICommodity
  {
    [Constructible]
    public Garlic(int amount = 1) : base(0xF84, amount)
    {
    }

    public Garlic(Serial serial) : base(serial)
    {
    }

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
