using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HealScroll : SpellScroll
{
    [Constructible]
    public HealScroll(int amount = 1) : base(3, 0x1F31, amount)
    {
    }
}
