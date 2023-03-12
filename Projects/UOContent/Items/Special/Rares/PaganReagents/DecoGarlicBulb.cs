using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoGarlicBulb : Item
{
    [Constructible]
    public DecoGarlicBulb() : base(0x18E3)
    {
        Movable = true;
        Stackable = false;
    }
}
