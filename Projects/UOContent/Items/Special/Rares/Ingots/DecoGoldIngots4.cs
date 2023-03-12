using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoGoldIngots4 : Item
{
    [Constructible]
    public DecoGoldIngots4() : base(0x1BEE)
    {
        Movable = true;
        Stackable = false;
    }
}
