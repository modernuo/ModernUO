using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MortarPestle : BaseTool
{
    [Constructible]
    public MortarPestle() : base(0xE9B) => Weight = 1.0;

    [Constructible]
    public MortarPestle(int uses) : base(uses, 0xE9B) => Weight = 1.0;

    public override CraftSystem CraftSystem => DefAlchemy.CraftSystem;
}
