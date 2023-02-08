using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HydraScale : Item
{
    [Constructible]
    public HydraScale() : base(0x26B4)
    {
        LootType = LootType.Blessed;
        Hue = 0xC2; // TODO check
    }

    public override int LabelNumber => 1074760; // A hydra scale.
}
