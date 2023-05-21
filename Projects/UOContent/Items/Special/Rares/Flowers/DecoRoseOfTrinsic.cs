using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoRoseOfTrinsic : Item
{
    [Constructible]
    public DecoRoseOfTrinsic() : base(0x234C)
    {
        Movable = true;
        Stackable = false;
    }
}
