using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Flippable(0x1030, 0x1031)]
[SerializationGenerator(0, false)]
public partial class JointingPlane : BaseTool
{
    [Constructible]
    public JointingPlane() : base(0x1030) => Weight = 2.0;

    [Constructible]
    public JointingPlane(int uses) : base(uses, 0x1030) => Weight = 2.0;

    public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;
}
