using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AbscessTail : Item
{
    [Constructible]
    public AbscessTail() : base(0x1A9D)
    {
        LootType = LootType.Blessed;
        Hue = 0x51D; // TODO check
    }

    public override int LabelNumber => 1074231; // Abscess' Tail
}
