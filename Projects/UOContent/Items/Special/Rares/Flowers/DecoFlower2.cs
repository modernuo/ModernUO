using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoFlower2 : Item
{
    [Constructible]
    public DecoFlower2() : base(0x18D9)
    {
        Movable = true;
        Stackable = false;
    }
}
