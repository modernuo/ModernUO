using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SummonFamiliarScroll : SpellScroll
{
    [Constructible]
    public SummonFamiliarScroll(int amount = 1) : base(111, 0x226B, amount)
    {
    }
}
