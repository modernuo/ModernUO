using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BladeSpiritsScroll : SpellScroll
{
    [Constructible]
    public BladeSpiritsScroll(int amount = 1) : base(32, 0x1F4D, amount)
    {
    }
}
