using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoTarot4 : Item
{
    [Constructible]
    public DecoTarot4() : base(0x12A8)
    {
        Movable = true;
        Stackable = false;
    }
}
