using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoRocks : Item
{
    [Constructible]
    public DecoRocks() : base(0x1367)
    {
        Movable = true;
        Stackable = false;
    }
}
