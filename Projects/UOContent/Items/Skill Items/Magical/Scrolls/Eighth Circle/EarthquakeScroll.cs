using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EarthquakeScroll : SpellScroll
{
    [Constructible]
    public EarthquakeScroll(int amount = 1) : base(56, 0x1F65, amount)
    {
    }
}
