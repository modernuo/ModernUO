using ModernUO.Serialization;
using Server.Engines.Harvest;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Shovel : BaseHarvestTool
{
    [Constructible]
    public Shovel(int uses = 50) : base(0xF39, uses)
    {
    }

    public override double DefaultWeight => 5.0;

    public override HarvestSystem HarvestSystem => Mining.System;
}
