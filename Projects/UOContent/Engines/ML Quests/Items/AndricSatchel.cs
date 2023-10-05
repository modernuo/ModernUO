using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AndricSatchel : Backpack
{
    [Constructible]
    public AndricSatchel()
    {
        Hue = Utility.RandomBrightHue();
        DropItem(new Feather(10));
        DropItem(new FletcherTools());
    }
}
