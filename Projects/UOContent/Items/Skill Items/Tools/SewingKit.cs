using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SewingKit : BaseTool
{
    [Constructible]
    public SewingKit() : base(0xF9D) => Weight = 2.0;

    [Constructible]
    public SewingKit(int uses) : base(uses, 0xF9D) => Weight = 2.0;

    public override CraftSystem CraftSystem => DefTailoring.CraftSystem;
}
