using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PoisonScroll : SpellScroll
{
    [Constructible]
    public PoisonScroll(int amount = 1) : base(19, 0x1F40, amount)
    {
    }
}
