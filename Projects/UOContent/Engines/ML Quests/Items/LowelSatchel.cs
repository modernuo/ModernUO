using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LowelSatchel : Backpack
{
    [Constructible]
    public LowelSatchel()
    {
        Hue = Utility.RandomBrightHue();
        DropItem(new Board(10));
        DropItem(new DovetailSaw());
    }
}
