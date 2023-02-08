using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BagOfSmokeBombs : Bag
{
    [Constructible]
    public BagOfSmokeBombs(int amount = 20)
    {
        for (var i = 0; i < amount; ++i)
        {
            DropItem(new SmokeBomb());
        }
    }
}
