using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ArchCureScroll : SpellScroll
{
    [Constructible]
    public ArchCureScroll(int amount = 1) : base(24, 0x1F45, amount)
    {
    }
}
