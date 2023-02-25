using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Flippable(0x102E, 0x102F)]
[SerializationGenerator(0, false)]
public partial class Nails : BaseTool
{
    [Constructible]
    public Nails() : base(0x102E) => Weight = 2.0;

    [Constructible]
    public Nails(int uses) : base(uses, 0x102C) => Weight = 2.0;

    public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;
}
