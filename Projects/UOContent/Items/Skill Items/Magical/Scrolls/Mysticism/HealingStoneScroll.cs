using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HealingStoneScroll : SpellScroll
{
    [Constructible]
    public HealingStoneScroll(int amount = 1) : base(678, 0x2D9F, amount)
    {
    }
}
