using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BagOfNecroReagents : Bag
{
    [Constructible]
    public BagOfNecroReagents(int amount = 50)
    {
        DropItem(new BatWing(amount));
        DropItem(new GraveDust(amount));
        DropItem(new DaemonBlood(amount));
        DropItem(new NoxCrystal(amount));
        DropItem(new PigIron(amount));
    }
}
