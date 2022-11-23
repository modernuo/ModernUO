using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class RingOfTheSavant : GoldRing
{
    [Constructible]
    public RingOfTheSavant()
    {
        LootType = LootType.Blessed;

        Attributes.BonusInt = 3;
        Attributes.CastRecovery = 1;
        Attributes.CastSpeed = 1;
    }

    public override int LabelNumber => 1077608; // Ring of the Savant
}
