using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class NightSightScroll : SpellScroll
{
    [Constructible]
    public NightSightScroll(int amount = 1) : base(5, 0x1F33, amount)
    {
    }
}
