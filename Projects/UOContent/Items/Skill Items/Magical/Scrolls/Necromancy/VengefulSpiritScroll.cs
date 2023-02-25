using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class VengefulSpiritScroll : SpellScroll
{
    [Constructible]
    public VengefulSpiritScroll(int amount = 1) : base(113, 0x226D, amount)
    {
    }
}
