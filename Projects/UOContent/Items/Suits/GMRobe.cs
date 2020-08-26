namespace Server.Items
{
  public class GMRobe : BaseSuit
  {
    [Constructible]
    public GMRobe() : base(AccessLevel.GameMaster, 0x26, 0x204F)
    {
    }

    public GMRobe(Serial serial) : base(serial)
    {
    }

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