namespace Server.Items
{
  public class BagOfSmokeBombs : Bag
  {
    [Constructible]
    public BagOfSmokeBombs(int amount = 20)
    {
      for (int i = 0; i < amount; ++i)
        DropItem(new SmokeBomb());
    }

    public BagOfSmokeBombs(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }
}
