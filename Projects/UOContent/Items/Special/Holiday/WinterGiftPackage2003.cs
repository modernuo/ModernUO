using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x232A, 0x232B)]
[SerializationGenerator(0)]
public partial class WinterGiftPackage2003 : GiftBox
{
    [Constructible]
    public WinterGiftPackage2003()
    {
        DropItem(new Snowman());
        DropItem(new WreathDeed());
        DropItem(new BlueSnowflake());
        DropItem(new RedPoinsettia());
    }
}
