using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoHorseDung : Item
{
    [Constructible]
    public DecoHorseDung() : base(0xF3B)
    {
        Movable = true;
        Stackable = false;
    }
}
