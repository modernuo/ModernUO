using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MagicArrowScroll : SpellScroll
{
    [Constructible]
    public MagicArrowScroll(int amount = 1) : base(4, 0x1F32, amount)
    {
    }
}
