using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoSilverIngots : Item
{
    [Constructible]
    public DecoSilverIngots() : base(0x1BFA)
    {
        Movable = true;
        Stackable = false;
    }
}
