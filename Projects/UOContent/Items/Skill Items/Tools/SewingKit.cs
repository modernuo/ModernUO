using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SewingKit : BaseTool
{
    [Constructible]
    public SewingKit() : base(0xF9D)
    {
    }

    [Constructible]
    public SewingKit(int uses) : base(uses, 0xF9D)
    {
    }

    public override double DefaultWeight => 2.0;

    public override CraftSystem CraftSystem => DefTailoring.CraftSystem;
}
