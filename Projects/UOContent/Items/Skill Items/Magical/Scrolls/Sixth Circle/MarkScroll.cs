using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MarkScroll : SpellScroll
{
    [Constructible]
    public MarkScroll(int amount = 1) : base(44, 0x1F59, amount)
    {
    }
}
