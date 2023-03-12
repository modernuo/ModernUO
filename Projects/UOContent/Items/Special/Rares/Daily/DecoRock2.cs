using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoRock2 : Item
{
    [Constructible]
    public DecoRock2() : base(0x1363)
    {
        Movable = true;
        Stackable = false;
    }
}
