namespace Server.Items
{
  public class Ginseng : BaseReagent, ICommodity
  {
    [Constructible]
    public Ginseng(int amount = 1) : base(0xF85, amount)
    {
    }

    public Ginseng(Serial serial) : base(serial)
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
