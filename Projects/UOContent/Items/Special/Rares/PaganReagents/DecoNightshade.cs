using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoNightshade : Item
{
    [Constructible]
    public DecoNightshade() : base(0x18E7)
    {
        Movable = true;
        Stackable = false;
    }
}
