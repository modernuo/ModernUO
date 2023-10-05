using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AndrosGratitude : SmithHammer
{
    [Constructible]
    public AndrosGratitude() : base(10) => LootType = LootType.Blessed;

    public override int LabelNumber => 1075345; // Andros Gratitude
}
