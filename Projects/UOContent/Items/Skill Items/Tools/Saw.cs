using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Flippable(0x1034, 0x1035)]
[SerializationGenerator(0, false)]
public partial class Saw : BaseTool
{
    [Constructible]
    public Saw() : base(0x1034) => Weight = 2.0;

    [Constructible]
    public Saw(int uses) : base(uses, 0x1034) => Weight = 2.0;

    public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;
}
