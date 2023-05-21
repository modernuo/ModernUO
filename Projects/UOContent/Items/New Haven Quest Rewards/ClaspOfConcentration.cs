using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class ClaspOfConcentration : SilverBracelet
{
    [Constructible]
    public ClaspOfConcentration()
    {
        LootType = LootType.Blessed;

        Attributes.RegenStam = 2;
        Attributes.RegenMana = 1;
        Resistances.Fire = 5;
        Resistances.Cold = 5;
    }

    public override int LabelNumber => 1077695; // Clasp of Concentration
}
