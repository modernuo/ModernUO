using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DrawKnife : BaseTool
{
    [Constructible]
    public DrawKnife() : base(0x10E4) => Weight = 1.0;

    [Constructible]
    public DrawKnife(int uses) : base(uses, 0x10E4) => Weight = 1.0;

    public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;
}
