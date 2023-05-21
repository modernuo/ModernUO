using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SpellTriggerScroll : SpellScroll
{
    [Constructible]
    public SpellTriggerScroll(int amount = 1) : base(685, 0x2DA6, amount)
    {
    }
}
