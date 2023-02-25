using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Flippable(0x0FBF, 0x0FC0)]
[SerializationGenerator(0, false)]
public partial class ScribesPen : BaseTool
{
    [Constructible]
    public ScribesPen() : base(0x0FBF) => Weight = 1.0;

    [Constructible]
    public ScribesPen(int uses) : base(uses, 0x0FBF) => Weight = 1.0;

    public override CraftSystem CraftSystem => DefInscription.CraftSystem;

    public override int LabelNumber => 1044168; // scribe's pen
}
