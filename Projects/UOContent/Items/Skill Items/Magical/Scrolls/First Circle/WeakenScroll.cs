using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class WeakenScroll : SpellScroll
{
    [Constructible]
    public WeakenScroll(int amount = 1) : base(7, 0x1F34, amount)
    {
    }
}
