using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FlamestrikeScroll : SpellScroll
{
    [Constructible]
    public FlamestrikeScroll(int amount = 1) : base(50, 0x1F5F, amount)
    {
    }
}
