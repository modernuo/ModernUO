using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SpecialTreatForDrithen : Item
{
    [Constructible]
    public SpecialTreatForDrithen() : base(0x21B)
    {
        LootType = LootType.Blessed;
        Hue = 0x489;
    }

    public override int LabelNumber => 1074517; // Special Treat for Drithen
}
