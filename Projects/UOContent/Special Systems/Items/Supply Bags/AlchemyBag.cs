using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AlchemyBag : Bag
{
    [Constructible]
    public AlchemyBag(int amount = 5000)
    {
        Hue = 0x250;
        DropItem(new MortarPestle(Math.Max(amount / 1000, 1)));
        DropItem(new BagOfReagents(5000));
        DropItem(new Bottle(5000));
    }

    public override string DefaultName => "an Alchemy Kit";
}
