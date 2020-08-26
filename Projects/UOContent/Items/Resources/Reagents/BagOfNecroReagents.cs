namespace Server.Items
{
  public class BagOfNecroReagents : Bag
  {
    [Constructible]
    public BagOfNecroReagents(int amount = 50)
    {
      DropItem(new BatWing(amount));
      DropItem(new GraveDust(amount));
      DropItem(new DaemonBlood(amount));
      DropItem(new NoxCrystal(amount));
      DropItem(new PigIron(amount));
    }

    public BagOfNecroReagents(Serial serial) : base(serial)
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
