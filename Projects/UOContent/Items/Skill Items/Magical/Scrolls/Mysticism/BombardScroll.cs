using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BombardScroll : SpellScroll
{
    [Constructible]
    public BombardScroll(int amount = 1) : base(688, 0x2DA9, amount)
    {
    }
}
