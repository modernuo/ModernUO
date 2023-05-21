using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SpellPlagueScroll : SpellScroll
{
    [Constructible]
    public SpellPlagueScroll(int amount = 1) : base(689, 0x2DAA, amount)
    {
    }
}
