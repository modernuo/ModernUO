using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoTarot6 : Item
{
    [Constructible]
    public DecoTarot6() : base(0x12AA)
    {
        Movable = true;
        Stackable = false;
    }
}
