using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoSilverIngots4 : Item
{
    [Constructible]
    public DecoSilverIngots4() : base(0x1BF9)
    {
        Movable = true;
        Stackable = false;
    }
}
