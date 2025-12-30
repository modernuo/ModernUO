using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Flippable(0x1022, 0x1023)]
[SerializationGenerator(0, false)]
public partial class FletcherTools : BaseTool
{
    [Constructible]
    public FletcherTools() : base(0x1022)
    {
    }

    [Constructible]
    public FletcherTools(int uses) : base(uses, 0x1022)
    {
    }

    public override double DefaultWeight => 2.0;

    public override CraftSystem CraftSystem => DefBowFletching.CraftSystem;
}
