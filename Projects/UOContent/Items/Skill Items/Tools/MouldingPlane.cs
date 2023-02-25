using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Flippable(0x102C, 0x102D)]
[SerializationGenerator(0, false)]
public partial class MouldingPlane : BaseTool
{
    [Constructible]
    public MouldingPlane() : base(0x102C) => Weight = 2.0;

    [Constructible]
    public MouldingPlane(int uses) : base(uses, 0x102C) => Weight = 2.0;

    public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;
}
