using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoGinsengRoot : Item
{
    [Constructible]
    public DecoGinsengRoot() : base(0x18EB)
    {
        Movable = true;
        Stackable = false;
    }
}
