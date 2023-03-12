using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoSpittoon : Item
{
    [Constructible]
    public DecoSpittoon() : base(0x1003)
    {
        Movable = true;
        Stackable = false;
    }
}
