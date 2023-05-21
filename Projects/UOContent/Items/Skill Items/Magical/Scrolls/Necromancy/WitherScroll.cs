using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class WitherScroll : SpellScroll
{
    [Constructible]
    public WitherScroll(int amount = 1) : base(114, 0x226E, amount)
    {
    }
}
