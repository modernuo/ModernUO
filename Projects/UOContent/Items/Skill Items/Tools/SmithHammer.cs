using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Flippable(0x13E3, 0x13E4)]
[SerializationGenerator(0, false)]
public partial class SmithHammer : BaseTool
{
    [Constructible]
    public SmithHammer() : base(0x13E3)
    {
        Weight = 8.0;
        Layer = Layer.OneHanded;
    }

    [Constructible]
    public SmithHammer(int uses) : base(uses, 0x13E3)
    {
        Weight = 8.0;
        Layer = Layer.OneHanded;
    }

    public override CraftSystem CraftSystem => DefBlacksmithy.CraftSystem;
}
