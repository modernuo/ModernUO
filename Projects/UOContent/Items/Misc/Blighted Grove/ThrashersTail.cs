using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ThrashersTail : Item
{
    [Constructible]
    public ThrashersTail() : base(0x1A9D)
    {
        LootType = LootType.Blessed;
        Hue = 0x455;
    }

    public override int LabelNumber => 1074230; // Thrasher's Tail
}
