using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoTarot5 : Item
{
    [Constructible]
    public DecoTarot5() : base(0x12A9)
    {
        Movable = true;
        Stackable = false;
    }
}
