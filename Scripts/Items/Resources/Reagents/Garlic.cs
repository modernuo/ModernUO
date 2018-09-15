namespace Server.Items
{
  public class Garlic : BaseReagent, ICommodity
  {
    [Constructible]
    public Garlic() : this(1)
    {
    }

    [Constructible]
    public Garlic(int amount) : base(0xF84, amount)
    {
    }

    public Garlic(Serial serial) : base(serial)
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