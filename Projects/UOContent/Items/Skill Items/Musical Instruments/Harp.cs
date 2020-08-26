namespace Server.Items
{
  public class Harp : BaseInstrument
  {
    [Constructible]
    public Harp() : base(0xEB1, 0x43, 0x44) => Weight = 35.0;

    public Harp(Serial serial) : base(serial)
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
        Weight = 35.0;
    }
  }
}