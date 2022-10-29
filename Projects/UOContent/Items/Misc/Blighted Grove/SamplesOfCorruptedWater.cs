using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SamplesOfCorruptedWater : Item
{
    [Constructible]
    public SamplesOfCorruptedWater() : base(0xEFE) => LootType = LootType.Blessed;

    public override int LabelNumber => 1074999; // samples of corrupted water
}
