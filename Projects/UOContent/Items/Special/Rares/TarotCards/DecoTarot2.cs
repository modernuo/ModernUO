using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoTarot2 : Item
{
    [Constructible]
    public DecoTarot2() : base(0x12A6)
    {
        Movable = true;
        Stackable = false;
    }
}
