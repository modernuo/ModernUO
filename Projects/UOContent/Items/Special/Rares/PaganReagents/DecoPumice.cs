using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoPumice : Item
{
    [Constructible]
    public DecoPumice() : base(0xF8B)
    {
        Movable = true;
        Stackable = false;
    }
}
