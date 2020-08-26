namespace Server.Items /* High seas, loot from merchant ship's hold, also a "uncommon" loot item */
{
  public class WhiteClothDyeTub : DyeTub
  {
    [Constructible]
    public WhiteClothDyeTub() => DyedHue = Hue = 0x9C2;

    public WhiteClothDyeTub(Serial serial)
      : base(serial)
    {
    }

    public override int LabelNumber => 1149984; // White Cloth Dye Tub

    public override bool Redyable => false;

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