using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x49CC, 0x49D0)]
    public partial class AnimatedHeartShapedBox : HeartShapedBox
    {
        [Constructible]
        public AnimatedHeartShapedBox() => ItemID = 0x49CC;
    }
}
