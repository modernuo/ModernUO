using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MindRotScroll : SpellScroll
{
    [Constructible]
    public MindRotScroll(int amount = 1) : base(107, 0x2267, amount)
    {
    }
}
