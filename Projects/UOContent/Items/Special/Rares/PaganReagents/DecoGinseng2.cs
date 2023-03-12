using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoGinseng2 : Item
{
    [Constructible]
    public DecoGinseng2() : base(0x18EA)
    {
        Movable = true;
        Stackable = false;
    }
}
