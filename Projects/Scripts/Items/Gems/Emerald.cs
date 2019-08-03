namespace Server.Items
{
  public class Emerald : Item
  {
    [Constructible]
    public Emerald(int amount = 1) : base(0xF10)
    {
      Stackable = true;
      Amount = amount;
    }

    public Emerald(Serial serial) : base(serial)
    {
    }

    public override double DefaultWeight => 0.1;


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
