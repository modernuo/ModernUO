using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EnergyVortexScroll : SpellScroll
{
    [Constructible]
    public EnergyVortexScroll(int amount = 1) : base(57, 0x1F66, amount)
    {
    }
}
