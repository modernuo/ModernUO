using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Cards3 : Item
{
    [Constructible]
    public Cards3() : base(0xE15)
    {
        Movable = true;
        Stackable = false;
    }
}
