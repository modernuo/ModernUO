using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Checkers2 : Item
{
    [Constructible]
    public Checkers2() : base(0xE1B)
    {
        Movable = true;
        Stackable = false;
    }
}
