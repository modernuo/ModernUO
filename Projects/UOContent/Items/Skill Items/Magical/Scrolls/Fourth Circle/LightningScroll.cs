using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LightningScroll : SpellScroll
{
    [Constructible]
    public LightningScroll(int amount = 1) : base(29, 0x1F4A, amount)
    {
    }
}
