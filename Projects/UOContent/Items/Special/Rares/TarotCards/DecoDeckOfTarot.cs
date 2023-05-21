using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoDeckOfTarot : Item
{
    [Constructible]
    public DecoDeckOfTarot() : base(0x12AB)
    {
        Movable = true;
        Stackable = false;
    }
}
