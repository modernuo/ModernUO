using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoGoldIngot : Item
{
    [Constructible]
    public DecoGoldIngot() : base(0x1BE9)
    {
        Movable = true;
        Stackable = true;
    }
}
