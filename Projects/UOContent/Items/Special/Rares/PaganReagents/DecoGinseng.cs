using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoGinseng : Item
{
    [Constructible]
    public DecoGinseng() : base(0x18E9)
    {
        Movable = true;
        Stackable = false;
    }
}
