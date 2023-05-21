using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Checkers : Item
{
    [Constructible]
    public Checkers() : base(0xE1A)
    {
        Movable = true;
        Stackable = false;
    }
}
