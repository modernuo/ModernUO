using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoGinsengRoot2 : Item
{
    [Constructible]
    public DecoGinsengRoot2() : base(0x18EC)
    {
        Movable = true;
        Stackable = false;
    }
}
