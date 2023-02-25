using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RecallScroll : SpellScroll
{
    [Constructible]
    public RecallScroll(int amount = 1) : base(31, 0x1F4C, amount)
    {
    }
}
