using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Froe : BaseTool
{
    [Constructible]
    public Froe() : base(0x10E5)
    {
    }

    [Constructible]
    public Froe(int uses) : base(uses, 0x10E5)
    {
    }

    public override double DefaultWeight => 1.0;

    public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;
}
