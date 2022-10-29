using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PrimitiveFetish : Item
{
    [Constructible]
    public PrimitiveFetish() : base(0x23F)
    {
        LootType = LootType.Blessed;
        Hue = 0x244;
    }

    public override int LabelNumber => 1074675; // Primitive Fetish
}
