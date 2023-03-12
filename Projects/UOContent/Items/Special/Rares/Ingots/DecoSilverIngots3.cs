using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoSilverIngots3 : Item
{
    [Constructible]
    public DecoSilverIngots3() : base(0x1BF7)
    {
        Movable = true;
        Stackable = false;
    }
}
