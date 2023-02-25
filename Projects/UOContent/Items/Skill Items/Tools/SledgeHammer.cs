using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Flippable(0xFB5, 0xFB4)]
[SerializationGenerator(0, false)]
public partial class SledgeHammer : BaseTool
{
    [Constructible]
    public SledgeHammer() : base(0xFB5) => Layer = Layer.OneHanded;

    [Constructible]
    public SledgeHammer(int uses) : base(uses, 0xFB5) => Layer = Layer.OneHanded;

    public override CraftSystem CraftSystem => DefBlacksmithy.CraftSystem;
}
