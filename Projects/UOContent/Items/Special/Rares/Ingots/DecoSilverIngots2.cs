using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoSilverIngots2 : Item
{
    [Constructible]
    public DecoSilverIngots2() : base(0x1BF6)
    {
        Movable = true;
        Stackable = false;
    }
}
