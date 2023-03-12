using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoNightshade3 : Item
{
    [Constructible]
    public DecoNightshade3() : base(0x18E6)
    {
        Movable = true;
        Stackable = false;
    }
}
