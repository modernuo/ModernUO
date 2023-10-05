using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GervisSatchel : Backpack
{
    [Constructible]
    public GervisSatchel()
    {
        Hue = Utility.RandomBrightHue();
        DropItem(new IronIngot(10));
        DropItem(new SmithHammer());
    }
}
