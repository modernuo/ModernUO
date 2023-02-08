using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SerpentFangSectBadge : Item
{
    [Constructible]
    public SerpentFangSectBadge() : base(0x23C) => LootType = LootType.Blessed;

    public override int LabelNumber => 1073139; // A Serpent Fang Sect Badge
}
