using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ExplosionScroll : SpellScroll
{
    [Constructible]
    public ExplosionScroll(int amount = 1) : base(42, 0x1F57, amount)
    {
    }
}
