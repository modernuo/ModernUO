using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HailStormScroll : SpellScroll
{
    [Constructible]
    public HailStormScroll(int amount = 1) : base(690, 0x2DAB, amount)
    {
    }
}
