using ModernUO.Serialization;
using Server.Engines.Harvest;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SturdyShovel : BaseHarvestTool
{
    [Constructible]
    public SturdyShovel(int uses = 180) : base(0xF39, uses)
    {
        Weight = 5.0;
        Hue = 0x973;
    }

    public override int LabelNumber => 1045125; // sturdy shovel
    public override HarvestSystem HarvestSystem => Mining.System;
}
