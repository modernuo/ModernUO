namespace Server.Items
{
  public class SulfurousAsh : BaseReagent, ICommodity
  {
    [Constructible]
    public SulfurousAsh(int amount = 1) : base(0xF8C, amount)
    {
    }

    public SulfurousAsh(Serial serial) : base(serial)
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
