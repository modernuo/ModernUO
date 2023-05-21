using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Flippable(0x1032, 0x1033)]
[SerializationGenerator(0, false)]
public partial class SmoothingPlane : BaseTool
{
    [Constructible]
    public SmoothingPlane() : base(0x1032) => Weight = 1.0;

    [Constructible]
    public SmoothingPlane(int uses) : base(uses, 0x1032) => Weight = 1.0;

    public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;
}
