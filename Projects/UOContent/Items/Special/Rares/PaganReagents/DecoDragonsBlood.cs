using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoDragonsBlood : Item
{
    [Constructible]
    public DecoDragonsBlood() : base(0x4077)
    {
        Movable = true;
        Stackable = false;
    }
}
