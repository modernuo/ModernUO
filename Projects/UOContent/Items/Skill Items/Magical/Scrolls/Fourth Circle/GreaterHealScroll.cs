using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GreaterHealScroll : SpellScroll
{
    [Constructible]
    public GreaterHealScroll(int amount = 1) : base(28, 0x1F49, amount)
    {
    }
}
