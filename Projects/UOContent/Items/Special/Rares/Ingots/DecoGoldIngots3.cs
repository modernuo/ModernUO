using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoGoldIngots3 : Item
{
    [Constructible]
    public DecoGoldIngots3() : base(0x1BED)
    {
        Movable = true;
        Stackable = false;
    }
}
