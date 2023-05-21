using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Flippable(0xfbb, 0xfbc)]
[SerializationGenerator(0, false)]
public partial class Tongs : BaseTool
{
    [Constructible]
    public Tongs() : base(0xFBB) => Weight = 2.0;

    [Constructible]
    public Tongs(int uses) : base(uses, 0xFBB) => Weight = 2.0;

    public override CraftSystem CraftSystem => DefBlacksmithy.CraftSystem;
}
