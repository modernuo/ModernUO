using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Skillet : BaseTool
{
    [Constructible]
    public Skillet() : base(0x97F) => Weight = 1.0;

    [Constructible]
    public Skillet(int uses) : base(uses, 0x97F) => Weight = 1.0;

    public override int LabelNumber => 1044567; // skillet

    public override CraftSystem CraftSystem => DefCooking.CraftSystem;
}
