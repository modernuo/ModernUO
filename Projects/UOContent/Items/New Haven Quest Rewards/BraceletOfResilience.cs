using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BraceletOfResilience : GoldBracelet
{
    [Constructible]
    public BraceletOfResilience()
    {
        LootType = LootType.Blessed;

        Attributes.DefendChance = 5;
        Resistances.Fire = 5;
        Resistances.Cold = 5;
        Resistances.Poison = 5;
        Resistances.Energy = 5;
    }

    public override int LabelNumber => 1077627; // Bracelet of Resilience
}
