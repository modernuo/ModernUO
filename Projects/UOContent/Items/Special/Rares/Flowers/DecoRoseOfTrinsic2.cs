using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoRoseOfTrinsic2 : Item
{
    [Constructible]
    public DecoRoseOfTrinsic2() : base(0x234D)
    {
        Movable = true;
        Stackable = false;
    }
}
