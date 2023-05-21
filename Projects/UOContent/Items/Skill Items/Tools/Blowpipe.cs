using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Flippable(0xE8A, 0xE89)]
[SerializationGenerator(0, false)]
public partial class Blowpipe : BaseTool
{
    [Constructible]
    public Blowpipe() : base(0xE8A)
    {
        Weight = 4.0;
        Hue = 0x3B9;
    }

    [Constructible]
    public Blowpipe(int uses) : base(uses, 0xE8A)
    {
        Weight = 4.0;
        Hue = 0x3B9;
    }

    public override CraftSystem CraftSystem => DefGlassblowing.CraftSystem;

    public override int LabelNumber => 1044608; // blow pipe
}
