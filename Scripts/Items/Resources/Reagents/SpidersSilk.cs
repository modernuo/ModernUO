namespace Server.Items
{
  public class SpidersSilk : BaseReagent, ICommodity
  {
    [Constructible]
    public SpidersSilk() : this(1)
    {
    }

    [Constructible]
    public SpidersSilk(int amount) : base(0xF8D, amount)
    {
    }

    public SpidersSilk(Serial serial) : base(serial)
    {
    }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;


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