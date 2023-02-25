using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class UnlockScroll : SpellScroll
{
    [Constructible]
    public UnlockScroll(int amount = 1) : base(22, 0x1F43, amount)
    {
    }
}
