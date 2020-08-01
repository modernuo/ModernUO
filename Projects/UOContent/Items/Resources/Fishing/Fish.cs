namespace Server.Items
{
  public class Fish : Item, ICarvable
  {
    [Constructible]
    public Fish(int amount = 1) : base(Utility.Random(0x09CC, 4))
    {
      Stackable = true;
      Weight = 1.0;
      Amount = amount;
    }

    public Fish(Serial serial) : base(serial)
    {
    }

    public void Carve(Mobile from, Item item)
    {
      ScissorHelper(from, new RawFishSteak(), 4);
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
