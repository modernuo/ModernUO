using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoRocks2 : Item
{
    [Constructible]
    public DecoRocks2() : base(0x136D)
    {
        Movable = true;
        Stackable = false;
    }
}
