using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Chessmen3 : Item
{
    [Constructible]
    public Chessmen3() : base(0xE14)
    {
        Movable = true;
        Stackable = false;
    }
}
