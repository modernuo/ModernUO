using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoRoseOfTrinsic3 : Item
{
    [Constructible]
    public DecoRoseOfTrinsic3() : base(0x234B)
    {
        Movable = true;
        Stackable = false;
    }
}
