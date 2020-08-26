namespace Server.Items
{
  public class BagOfAllReagents : Bag
  {
    [Constructible]
    public BagOfAllReagents(int amount = 50)
    {
      DropItem(new BlackPearl(amount));
      DropItem(new Bloodmoss(amount));
      DropItem(new Garlic(amount));
      DropItem(new Ginseng(amount));
      DropItem(new MandrakeRoot(amount));
      DropItem(new Nightshade(amount));
      DropItem(new SulfurousAsh(amount));
      DropItem(new SpidersSilk(amount));
      DropItem(new BatWing(amount));
      DropItem(new GraveDust(amount));
      DropItem(new DaemonBlood(amount));
      DropItem(new NoxCrystal(amount));
      DropItem(new PigIron(amount));
    }

    public BagOfAllReagents(Serial serial) : base(serial)
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
