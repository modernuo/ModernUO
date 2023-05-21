using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DragonFlameSectBadge : Item
{
    [Constructible]
    public DragonFlameSectBadge() : base(0x23E) => LootType = LootType.Blessed;

    public override int LabelNumber => 1073141; // A Dragon Flame Sect Badge
}
