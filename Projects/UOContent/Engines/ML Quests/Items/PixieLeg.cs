using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PixieLeg : ChickenLeg
{
    [Constructible]
    public PixieLeg(int amount = 1) : base(amount)
    {
        LootType = LootType.Blessed;
        Hue = 0x1C2;
    }

    public override int LabelNumber => 1074613; // Pixie Leg
}
