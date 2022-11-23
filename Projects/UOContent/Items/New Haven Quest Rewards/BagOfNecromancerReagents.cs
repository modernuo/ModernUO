using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BagOfNecromancerReagents : Bag
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
}
