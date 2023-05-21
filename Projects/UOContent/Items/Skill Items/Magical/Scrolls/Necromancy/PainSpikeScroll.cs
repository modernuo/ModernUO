using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PainSpikeScroll : SpellScroll
{
    [Constructible]
    public PainSpikeScroll(int amount = 1) : base(108, 0x2268, amount)
    {
    }
}
