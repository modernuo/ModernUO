using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CoilsFang : Item
{
    [Constructible]
    public CoilsFang() : base(0x10E8)
    {
        LootType = LootType.Blessed;
        Hue = 0x487;
    }

    public override int LabelNumber => 1074229; // Coil's Fang
}
