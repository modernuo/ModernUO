using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EnergyFieldScroll : SpellScroll
{
    [Constructible]
    public EnergyFieldScroll(int amount = 1) : base(49, 0x1F5E, amount)
    {
    }
}
