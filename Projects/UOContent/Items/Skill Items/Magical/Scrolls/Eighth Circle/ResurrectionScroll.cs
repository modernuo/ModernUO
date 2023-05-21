using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ResurrectionScroll : SpellScroll
{
    [Constructible]
    public ResurrectionScroll(int amount = 1) : base(58, 0x1F67, amount)
    {
    }
}
