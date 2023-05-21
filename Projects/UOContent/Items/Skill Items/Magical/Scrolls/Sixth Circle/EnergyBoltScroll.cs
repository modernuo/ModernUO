using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EnergyBoltScroll : SpellScroll
{
    [Constructible]
    public EnergyBoltScroll(int amount = 1) : base(41, 0x1F56, amount)
    {
    }
}
