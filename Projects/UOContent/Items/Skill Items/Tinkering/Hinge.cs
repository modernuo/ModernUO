namespace Server.Items
{
  [Flippable(0x1055, 0x1056)]
  public class Hinge : Item
  {
    [Constructible]
    public Hinge(int amount = 1) : base(0x1055)
    {
      Stackable = true;
      Amount = amount;
      Weight = 1.0;
    }

    public Hinge(Serial serial) : base(serial)
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
