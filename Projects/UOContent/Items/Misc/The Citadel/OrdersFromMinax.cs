using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class OrdersFromMinax : Item
{
    [Constructible]
    public OrdersFromMinax() : base(0x2279) => LootType = LootType.Blessed;

    public override int LabelNumber => 1074639; // Orders from Minax
}
