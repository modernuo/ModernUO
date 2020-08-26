namespace Server.Items
{
  public class TambourineTassel : BaseInstrument
  {
    [Constructible]
    public TambourineTassel() : base(0xE9E, 0x52, 0x53) => Weight = 1.0;

    public TambourineTassel(Serial serial) : base(serial)
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

      if (Weight == 2.0)
        Weight = 1.0;
    }
  }
}