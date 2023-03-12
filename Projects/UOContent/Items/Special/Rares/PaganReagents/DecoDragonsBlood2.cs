using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoDragonsBlood2 : Item
{
    [Constructible]
    public DecoDragonsBlood2() : base(0xF82)
    {
        Movable = true;
        Stackable = false;
    }
}
