using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Flippable(0xfbb, 0xfbc)]
[SerializationGenerator(0, false)]
public partial class Tongs : BaseTool
{
    [Constructible]
    public Tongs() : base(0xFBB)
    {
    }

    [Constructible]
    public Tongs(int uses) : base(uses, 0xFBB)
    {
    }

    public override double DefaultWeight => 2.0;

    public override CraftSystem CraftSystem => DefBlacksmithy.CraftSystem;
}
