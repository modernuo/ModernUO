using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TailorBag : Bag
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

    public override string DefaultName => "a Tailoring Kit";
}
