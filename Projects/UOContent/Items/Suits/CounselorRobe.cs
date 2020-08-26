namespace Server.Items
{
  public class CounselorRobe : BaseSuit
  {
    [Constructible]
    public CounselorRobe() : base(AccessLevel.Counselor, 0x3, 0x204F)
    {
    }

    public CounselorRobe(Serial serial) : base(serial)
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