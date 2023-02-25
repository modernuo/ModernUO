using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class InvisibilityScroll : SpellScroll
{
    [Constructible]
    public InvisibilityScroll(int amount = 1) : base(43, 0x1F58, amount)
    {
    }
}
