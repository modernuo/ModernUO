namespace Server.Items
{
  public class MandrakeRoot : BaseReagent, ICommodity
  {
    [Constructible]
    public MandrakeRoot(int amount = 1) : base(0xF86, amount)
    {
    }

    public MandrakeRoot(Serial serial) : base(serial)
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
