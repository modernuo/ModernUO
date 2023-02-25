using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SummonDaemonScroll : SpellScroll
{
    [Constructible]
    public SummonDaemonScroll(int amount = 1) : base(60, 0x1F69, amount)
    {
    }
}
