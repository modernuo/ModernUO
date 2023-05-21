using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AnimateDeadScroll : SpellScroll
{
    [Constructible]
    public AnimateDeadScroll(int amount = 1) : base(100, 0x2260, amount)
    {
    }
}
