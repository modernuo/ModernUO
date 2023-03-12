using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoSilverIngot2 : Item
{
    [Constructible]
    public DecoSilverIngot2() : base(0x1BF8)
    {
        Movable = true;
        Stackable = false;
    }
}
