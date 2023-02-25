using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MagicLockScroll : SpellScroll
{
    [Constructible]
    public MagicLockScroll(int amount = 1) : base(18, 0x1F3F, amount)
    {
    }
}
