using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class KirinBrains : Item
{
    [Constructible]
    public KirinBrains() : base(0x1CF0)
    {
        LootType = LootType.Blessed;
        Hue = 0xD7;
    }

    public override int LabelNumber => 1074612; // Ki-Rin Brains
}
