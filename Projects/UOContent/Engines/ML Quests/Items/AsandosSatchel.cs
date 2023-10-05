using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AsandosSatchel : Backpack
{
    [Constructible]
    public AsandosSatchel()
    {
        Hue = Utility.RandomBrightHue();
        DropItem(new SackFlour());
        DropItem(new Skillet());
    }
}
