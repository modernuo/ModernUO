using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MassSleepScroll : SpellScroll
{
    [Constructible]
    public MassSleepScroll(int amount = 1) : base(686, 0x2DA7, amount)
    {
    }
}
