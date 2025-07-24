using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RollingPin : BaseTool
{
    [Constructible]
    public RollingPin() : base(0x1043)
    {
    }

    [Constructible]
    public RollingPin(int uses) : base(uses, 0x1043)
    {
    }

    public override double DefaultWeight => 1.0;

    public override CraftSystem CraftSystem => DefCooking.CraftSystem;
}
