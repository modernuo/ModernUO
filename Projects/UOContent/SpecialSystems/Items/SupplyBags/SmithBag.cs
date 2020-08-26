namespace Server.Items
{
  public class SmithBag : Bag
  {
    [Constructible]
    public SmithBag(int amount = 5000)
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
      DropItem(new Tongs(amount));
      DropItem(new TinkerTools(amount));
    }

    public SmithBag(Serial serial) : base(serial)
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
