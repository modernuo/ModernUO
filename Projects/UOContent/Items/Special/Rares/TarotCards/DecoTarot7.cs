using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoTarot7 : Item
{
    [Constructible]
    public DecoTarot7() : base(0x12A5)
    {
        Movable = true;
        Stackable = false;
    }
}
