using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoGoldIngots : Item
{
    [Constructible]
    public DecoGoldIngots() : base(0x1BEA)
    {
        Movable = true;
        Stackable = false;
    }
}
