namespace Server.Items
{
  public class BagOfingots : Bag
  {
    [Constructible]
    public BagOfingots(int amount = 5000)
    {
      DropItem(new DullCopperIngot(amount));
      DropItem(new ShadowIronIngot(amount));
      DropItem(new CopperIngot(amount));
      DropItem(new BronzeIngot(amount));
      DropItem(new GoldIngot(amount));
      DropItem(new AgapiteIngot(amount));
      DropItem(new VeriteIngot(amount));
      DropItem(new ValoriteIngot(amount));
      DropItem(new IronIngot(amount));
      DropItem(new Tongs());
      DropItem(new TinkerTools());
    }

    public BagOfingots(Serial serial) : base(serial)
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
