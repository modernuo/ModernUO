using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoSilverIngots5 : Item
{
    [Constructible]
    public DecoSilverIngots5() : base(0x1BFA)
    {
        Movable = true;
        Stackable = false;
    }
}
