using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Cards : Item
{
    [Constructible]
    public Cards() : base(0xE19)
    {
        Movable = true;
        Stackable = false;
    }
}
