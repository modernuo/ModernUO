using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MuggSatchel : Backpack
{
    [Constructible]
    public MuggSatchel()
    {
        Hue = Utility.RandomBrightHue();
        DropItem(new Pickaxe());
        DropItem(new Pickaxe());
    }
}
