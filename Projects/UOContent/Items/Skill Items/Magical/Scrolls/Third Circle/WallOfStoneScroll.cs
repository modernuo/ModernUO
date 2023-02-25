using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class WallOfStoneScroll : SpellScroll
{
    [Constructible]
    public WallOfStoneScroll(int amount = 1) : base(23, 0x1F44, amount)
    {
    }
}
