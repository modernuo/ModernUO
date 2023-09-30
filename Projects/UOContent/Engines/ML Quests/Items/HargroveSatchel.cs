using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HargroveSatchel : Backpack
{
    [Constructible]
    public HargroveSatchel()
    {
        Hue = Utility.RandomBrightHue();
        DropItem(new Gold(15));
        DropItem(new Hatchet());
    }
}
