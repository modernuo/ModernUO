using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MortarPestle : BaseTool
{
    [Constructible]
    public MortarPestle() : base(0xE9B)
    {
    }

    [Constructible]
    public MortarPestle(int uses) : base(uses, 0xE9B)
    {
    }

    public override double DefaultWeight => 1.0;

    public override CraftSystem CraftSystem => DefAlchemy.CraftSystem;
}
