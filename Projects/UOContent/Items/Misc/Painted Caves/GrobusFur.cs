using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GrobusFur : Item
{
    [Constructible]
    public GrobusFur() : base(0x11F4)
    {
        LootType = LootType.Blessed;
        Hue = 0x455;
    }

    public override int LabelNumber => 1074676; // Grobu's Fur
}
