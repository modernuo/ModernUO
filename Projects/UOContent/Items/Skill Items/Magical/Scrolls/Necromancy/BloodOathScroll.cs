using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BloodOathScroll : SpellScroll
{
    [Constructible]
    public BloodOathScroll(int amount = 1) : base(101, 0x2261, amount)
    {
    }
}
