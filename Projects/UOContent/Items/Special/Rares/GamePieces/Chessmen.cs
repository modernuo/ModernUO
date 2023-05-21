using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Chessmen : Item
{
    [Constructible]
    public Chessmen() : base(0xE13)
    {
        Movable = true;
        Stackable = false;
    }
}
