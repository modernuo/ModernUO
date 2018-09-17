namespace Server.Items
{
  public class DragonsBlood : BaseReagent, ICommodity
  {
    [Constructible]
    public DragonsBlood()
      : this(1)
    {
    }

    [Constructible]
    public DragonsBlood(int amount)
      : base(0x4077, amount)
    {
    }

    public DragonsBlood(Serial serial)
      : base(serial)
    {
    }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => Core.ML;

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