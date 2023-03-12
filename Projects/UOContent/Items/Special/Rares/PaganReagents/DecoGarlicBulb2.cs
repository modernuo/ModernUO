using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoGarlicBulb2 : Item
{
    [Constructible]
    public DecoGarlicBulb2() : base(0x18E4)
    {
        Movable = true;
        Stackable = false;
    }
}
