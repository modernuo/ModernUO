using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TaintedSeeds : Item
{
    [Constructible]
    public TaintedSeeds() : base(0xDFA)
    {
        LootType = LootType.Blessed;
        Hue = 0x48; // TODO check
    }

    public override int LabelNumber => 1074233; // Tainted Seeds
}
