using System;

namespace Server.Items
{
  public class AlchemyBag : Bag
  {
    [Constructible]
    public AlchemyBag(int amount = 5000)
    {
      Hue = 0x250;
      DropItem(new MortarPestle(Math.Max(amount / 1000, 1)));
      DropItem(new BagOfReagents(5000));
      DropItem(new Bottle(5000));
    }

    public AlchemyBag(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "an Alchemy Kit";

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
