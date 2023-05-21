using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BagOfReagents : Bag
{
    [Constructible]
    public BagOfReagents(int amount = 50)
    {
        DropItem(new BlackPearl(amount));
        DropItem(new Bloodmoss(amount));
        DropItem(new Garlic(amount));
        DropItem(new Ginseng(amount));
        DropItem(new MandrakeRoot(amount));
        DropItem(new Nightshade(amount));
        DropItem(new SulfurousAsh(amount));
        DropItem(new SpidersSilk(amount));
    }
}
