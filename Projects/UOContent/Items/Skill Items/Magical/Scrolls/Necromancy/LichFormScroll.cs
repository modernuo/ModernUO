using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LichFormScroll : SpellScroll
{
    [Constructible]
    public LichFormScroll(int amount = 1) : base(106, 0x2266, amount)
    {
    }
}
