using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Flippable(0x1028, 0x1029)]
[SerializationGenerator(0, false)]
public partial class DovetailSaw : BaseTool
{
    [Constructible]
    public DovetailSaw() : base(0x1028) => Weight = 2.0;

    [Constructible]
    public DovetailSaw(int uses) : base(uses, 0x1028) => Weight = 2.0;

    public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;
}
