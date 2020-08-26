namespace Server.Items
{
  [Flippable(0x1059, 0x105A)]
  public class SextantParts : Item
  {
    [Constructible]
    public SextantParts(int amount = 1) : base(0x1059)
    {
      Stackable = true;
      Amount = amount;
      Weight = 2.0;
    }

    public SextantParts(Serial serial) : base(serial)
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
