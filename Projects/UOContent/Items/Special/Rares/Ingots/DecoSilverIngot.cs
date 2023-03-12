using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoSilverIngot : Item
{
    [Constructible]
    public DecoSilverIngot() : base(0x1BF5)
    {
        Movable = true;
        Stackable = true;
    }
}
