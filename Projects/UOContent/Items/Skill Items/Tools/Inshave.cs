using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Inshave : BaseTool
{
    [Constructible]
    public Inshave() : base(0x10E6) => Weight = 1.0;

    [Constructible]
    public Inshave(int uses) : base(uses, 0x10E6) => Weight = 1.0;

    public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;
}
