using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoNightshade2 : Item
{
    [Constructible]
    public DecoNightshade2() : base(0x18E5)
    {
        Movable = true;
        Stackable = false;
    }
}
