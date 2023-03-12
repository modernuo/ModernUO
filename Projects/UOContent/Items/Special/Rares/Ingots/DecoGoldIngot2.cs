using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoGoldIngot2 : Item
{
    [Constructible]
    public DecoGoldIngot2() : base(0x1BEC)
    {
        Movable = true;
        Stackable = false;
    }
}
