using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class QuiverOfInfinity : BaseQuiver
{
    [Constructible]
    public QuiverOfInfinity() : base(0x2B02)
    {
        LootType = LootType.Blessed;

        WeightReduction = 30;
        LowerAmmoCost = 20;

        Attributes.DefendChance = 5;
    }

    public override double DefaultWeight => 8.0;

    public override int LabelNumber => 1075201; // Quiver of Infinity
}
