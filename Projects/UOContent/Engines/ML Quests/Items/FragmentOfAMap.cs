using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FragmentOfAMap : Item
{
    [Constructible]
    public FragmentOfAMap() : base(0x14ED) => LootType = LootType.Blessed;

    public override int LabelNumber => 1074533; // Fragment of a Map
}
