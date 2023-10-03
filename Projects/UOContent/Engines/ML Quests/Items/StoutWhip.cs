using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class StoutWhip : Item
{
    [Constructible]
    public StoutWhip() : base(0x166F) => LootType = LootType.Blessed;

    public override int LabelNumber => 1074812; // Stout Whip
}
