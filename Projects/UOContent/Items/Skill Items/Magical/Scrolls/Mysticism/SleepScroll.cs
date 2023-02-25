using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SleepScroll : SpellScroll
{
    [Constructible]
    public SleepScroll(int amount = 1) : base(681, 0x2DA2, amount)
    {
    }
}
