using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SadrahSatchel : Backpack
{
    [Constructible]
    public SadrahSatchel()
    {
        Hue = Utility.RandomBrightHue();
        DropItem(new Bloodmoss(10));
        DropItem(new MortarPestle());
    }
}
