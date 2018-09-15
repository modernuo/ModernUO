namespace Server.Items
{
  public class MrPlainsCookies : Food
  {
    [Constructible]
    public MrPlainsCookies()
      : base(0x160C)
    {
      Weight = 1.0;
      FillFactor = 4;
      Hue = 0xF4;
    }

    public MrPlainsCookies(Serial serial)
      : base(serial)
    {
    }

    public override string DefaultName => "Mr Plain's Cookies";

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