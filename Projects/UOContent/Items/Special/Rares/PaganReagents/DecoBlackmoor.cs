using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoBlackmoor : Item
{
    [Constructible]
    public DecoBlackmoor() : base(0xF79)
    {
        Movable = true;
        Stackable = false;
    }
}
