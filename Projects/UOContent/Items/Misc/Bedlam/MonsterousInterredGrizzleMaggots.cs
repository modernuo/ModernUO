using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MonsterousInterredGrizzleMaggots : Item
{
    [Constructible]
    public MonsterousInterredGrizzleMaggots() : base(0x2633)
    {
    }

    public override int LabelNumber => 1075090; // Monsterous Interred Grizzle Maggots
}
