using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoTarot : Item
{
    [Constructible]
    public DecoTarot() : base(0x12A5)
    {
        Movable = true;
        Stackable = false;
    }
}
