using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoDeckOfTarot2 : Item
{
    [Constructible]
    public DecoDeckOfTarot2() : base(0x12Ac)
    {
        Movable = true;
        Stackable = false;
    }
}
