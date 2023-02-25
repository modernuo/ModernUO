using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MagicUnTrapScroll : SpellScroll
{
    [Constructible]
    public MagicUnTrapScroll(int amount = 1) : base(13, 0x1F3A, amount)
    {
    }
}
