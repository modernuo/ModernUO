using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoFlower : Item
{
    [Constructible]
    public DecoFlower() : base(0x18DA)
    {
        Movable = true;
        Stackable = false;
    }
}
