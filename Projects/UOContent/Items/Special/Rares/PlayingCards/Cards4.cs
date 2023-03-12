using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Cards4 : Item
{
    [Constructible]
    public Cards4() : base(0xE17)
    {
        Movable = true;
        Stackable = false;
    }
}
