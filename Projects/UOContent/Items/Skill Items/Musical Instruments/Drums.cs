namespace Server.Items
{
  public class Drums : BaseInstrument
  {
    [Constructible]
    public Drums() : base(0xE9C, 0x38, 0x39) => Weight = 4.0;

    public Drums(Serial serial) : base(serial)
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
        Weight = 4.0;
    }
  }
}