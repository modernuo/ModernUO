using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Hammer : BaseTool
{
    [Constructible]
    public Hammer() : base(0x102A)
    {
    }

    [Constructible]
    public Hammer(int uses) : base(uses, 0x102A)
    {
    }

    public override double DefaultWeight => 2.0;

    public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;
}
