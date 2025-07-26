using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Scorp : BaseTool
{
    [Constructible]
    public Scorp() : base(0x10E7)
    {
    }

    [Constructible]
    public Scorp(int uses) : base(uses, 0x10E7)
    {
    }

    public override double DefaultWeight => 1.0;

    public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;
}
