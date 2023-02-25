using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GateTravelScroll : SpellScroll
{
    [Constructible]
    public GateTravelScroll(int amount = 1) : base(51, 0x1F60, amount)
    {
    }
}
