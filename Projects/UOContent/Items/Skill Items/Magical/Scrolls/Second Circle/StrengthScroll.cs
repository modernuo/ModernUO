using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class StrengthScroll : SpellScroll
{
    [Constructible]
    public StrengthScroll(int amount = 1) : base(15, 0x1F3C, amount)
    {
    }
}
