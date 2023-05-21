using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MagicTrapScroll : SpellScroll
{
    [Constructible]
    public MagicTrapScroll(int amount = 1) : base(12, 0x1F39, amount)
    {
    }
}
