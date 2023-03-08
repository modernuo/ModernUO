using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class WristWatch : Clock
{
    [Constructible]
    public WristWatch() : base(0x1086)
    {
        Weight = DefaultWeight;
        LootType = LootType.Blessed;
        Layer = Layer.Bracelet;
    }

    public override int LabelNumber => 1041421; // a wrist watch
    public override double DefaultWeight => 1.0;
}
