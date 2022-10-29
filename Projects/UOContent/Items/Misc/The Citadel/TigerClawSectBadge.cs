using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TigerClawSectBadge : Item
{
    [Constructible]
    public TigerClawSectBadge() : base(0x23D) => LootType = LootType.Blessed;

    public override int LabelNumber => 1073140; // A Tiger Claw Sect Badge
}
