namespace Server.Items
{
  public class BatWing : BaseReagent, ICommodity
  {
    [Constructible]
    public BatWing(int amount = 1) : base(0xF78, amount)
    {
    }

    public BatWing(Serial serial) : base(serial)
    {
    }

    public int DescriptionNumber => LabelNumber;
    public bool IsDeedable => true;

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
