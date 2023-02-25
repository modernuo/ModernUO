using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Flippable(0x0FBF, 0x0FC0)]
[SerializationGenerator(0, false)]
public partial class MapmakersPen : BaseTool
{
    [Constructible]
    public MapmakersPen() : base(0x0FBF) => Weight = 1.0;

    [Constructible]
    public MapmakersPen(int uses) : base(uses, 0x0FBF) => Weight = 1.0;

    public override CraftSystem CraftSystem => DefCartography.CraftSystem;

    public override int LabelNumber => 1044167; // mapmaker's pen
}
