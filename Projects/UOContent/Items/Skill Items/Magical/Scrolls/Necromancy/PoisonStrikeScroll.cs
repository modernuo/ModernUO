using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PoisonStrikeScroll : SpellScroll
{
    [Constructible]
    public PoisonStrikeScroll(int amount = 1) : base(109, 0x2269, amount)
    {
    }
}
