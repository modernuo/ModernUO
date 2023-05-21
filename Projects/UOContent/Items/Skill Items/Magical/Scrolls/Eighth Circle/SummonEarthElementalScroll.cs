using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SummonEarthElementalScroll : SpellScroll
{
    [Constructible]
    public SummonEarthElementalScroll(int amount = 1) : base(61, 0x1F6A, amount)
    {
    }
}
