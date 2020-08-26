namespace Server.Items
{
  public class BrokenChair : Item
  {
    [Constructible]
    public BrokenChair() : base(Utility.Random(2) + 0xC19)
    {
      Movable = true;
      Stackable = false;
    }

    public BrokenChair(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}