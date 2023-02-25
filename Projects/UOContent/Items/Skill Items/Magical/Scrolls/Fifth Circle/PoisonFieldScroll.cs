using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PoisonFieldScroll : SpellScroll
{
    [Constructible]
    public PoisonFieldScroll(int amount = 1) : base(38, 0x1F53, amount)
    {
    }
}
