using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class NetherCycloneScroll : SpellScroll
{
    [Constructible]
    public NetherCycloneScroll(int amount = 1) : base(691, 0x2DAC, amount)
    {
    }
}
