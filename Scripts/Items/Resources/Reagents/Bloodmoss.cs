namespace Server.Items
{
  public class Bloodmoss : BaseReagent, ICommodity
  {
    [Constructible]
    public Bloodmoss() : this(1)
    {
    }

    [Constructible]
    public Bloodmoss(int amount) : base(0xF7B, amount)
    {
    }

    public Bloodmoss(Serial serial) : base(serial)
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