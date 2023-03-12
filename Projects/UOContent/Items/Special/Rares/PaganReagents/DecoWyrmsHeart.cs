using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoWyrmsHeart : Item
{
    [Constructible]
    public DecoWyrmsHeart() : base(0xF91)
    {
        Movable = true;
        Stackable = false;
    }
}
