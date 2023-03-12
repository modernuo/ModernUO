using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Cards2 : Item
{
    [Constructible]
    public Cards2() : base(0xE16)
    {
        Movable = true;
        Stackable = false;
    }
}
