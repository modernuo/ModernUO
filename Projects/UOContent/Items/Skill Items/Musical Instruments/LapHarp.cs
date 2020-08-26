namespace Server.Items
{
  public class LapHarp : BaseInstrument
  {
    [Constructible]
    public LapHarp() : base(0xEB2, 0x45, 0x46) => Weight = 10.0;

    public LapHarp(Serial serial) : base(serial)
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

      if (Weight == 3.0)
        Weight = 10.0;
    }
  }
}