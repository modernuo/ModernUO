using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EvilOmenScroll : SpellScroll
{
    [Constructible]
    public EvilOmenScroll(int amount = 1) : base(104, 0x2264, amount)
    {
    }
}
