using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CleansingWindsScroll : SpellScroll
{
    [Constructible]
    public CleansingWindsScroll(int amount = 1) : base(687, 0x2DA8, amount)
    {
    }
}
