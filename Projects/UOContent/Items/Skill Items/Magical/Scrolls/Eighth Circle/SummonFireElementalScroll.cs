using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SummonFireElementalScroll : SpellScroll
{
    [Constructible]
    public SummonFireElementalScroll(int amount = 1) : base(62, 0x1F6B, amount)
    {
    }
}
