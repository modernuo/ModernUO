using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SummonCreatureScroll : SpellScroll
{
    [Constructible]
    public SummonCreatureScroll(int amount = 1) : base(39, 0x1F54, amount)
    {
    }
}
