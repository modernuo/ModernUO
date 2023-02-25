using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RevealScroll : SpellScroll
{
    [Constructible]
    public RevealScroll(int amount = 1) : base(47, 0x1F5C, amount)
    {
    }
}
