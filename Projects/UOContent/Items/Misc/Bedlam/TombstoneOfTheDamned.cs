using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TombstoneOfTheDamned : Item
{
    [Constructible]
    public TombstoneOfTheDamned() : base(Utility.RandomMinMax(0xED7, 0xEDE))
    {
    }

    public override int LabelNumber => 1072123; // Tombstone of the Damned
}
