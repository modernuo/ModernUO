namespace Server.Items
{
  public class BagOfNecromancerReagents : Bag
  {
    [Constructible]
    public BagOfNecromancerReagents(int amount = 50)
    {
      DropItem(new BatWing(amount));
      DropItem(new GraveDust(amount));
      DropItem(new DaemonBlood(amount));
      DropItem(new NoxCrystal(amount));
      DropItem(new PigIron(amount));
    }

    public BagOfNecromancerReagents(Serial serial) : base(serial)
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
