using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoTarot3 : Item
{
    [Constructible]
    public DecoTarot3() : base(0x12A7)
    {
        Movable = true;
        Stackable = false;
    }
}
