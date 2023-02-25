using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ChainLightningScroll : SpellScroll
{
    [Constructible]
    public ChainLightningScroll(int amount = 1) : base(48, 0x1F5D, amount)
    {
    }
}
