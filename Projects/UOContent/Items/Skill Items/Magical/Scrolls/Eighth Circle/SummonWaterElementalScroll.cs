using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SummonWaterElementalScroll : SpellScroll
{
    [Constructible]
    public SummonWaterElementalScroll(int amount = 1) : base(63, 0x1F6C, amount)
    {
    }
}
