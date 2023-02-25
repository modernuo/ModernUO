using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SummonAirElementalScroll : SpellScroll
{
    [Constructible]
    public SummonAirElementalScroll(int amount = 1) : base(59, 0x1F68, amount)
    {
    }
}
