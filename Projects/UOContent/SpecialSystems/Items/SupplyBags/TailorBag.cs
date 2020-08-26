using System;

namespace Server.Items
{
  public class TailorBag : Bag
  {
    [Constructible]
    public TailorBag(int amount = 500)
    {
      Hue = 0x315;
      DropItem(new SewingKit(Math.Max(amount / 100, 1)));
      DropItem(new Scissors());
      DropItem(new Hides(amount));
      DropItem(new BoltOfCloth(Math.Max(amount / 25, 1)));
      DropItem(new DyeTub());
      DropItem(new DyeTub());
      DropItem(new BlackDyeTub());
      DropItem(new Dyes());
    }

    public TailorBag(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "a Tailoring Kit";

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
