namespace Server.Mobiles
{
  public class CrystalWisp : Wisp
  {
    [Constructible]
    public CrystalWisp()
    {
      Hue = 0x482;

      PackArcaneScroll(0, 1);
    }

    public CrystalWisp(Serial serial)
      : base(serial)
    {
    }

    public override string DefaultName => "a crystal wisp";

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