using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Bleach : PigmentsOfTokuno
{
    [Constructible]
    public Bleach() => LootType = LootType.Blessed;

    public override int LabelNumber => 1075375; // Bleach
}
