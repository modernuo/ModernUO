using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Chessmen2 : Item
{
    [Constructible]
    public Chessmen2() : base(0xE12)
    {
        Movable = true;
        Stackable = false;
    }
}
