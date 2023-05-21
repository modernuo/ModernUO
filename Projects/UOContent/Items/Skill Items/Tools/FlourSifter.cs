using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FlourSifter : BaseTool
{
    [Constructible]
    public FlourSifter() : base(0x103E) => Weight = 1.0;

    [Constructible]
    public FlourSifter(int uses) : base(uses, 0x103E) => Weight = 1.0;

    public override CraftSystem CraftSystem => DefCooking.CraftSystem;
}
