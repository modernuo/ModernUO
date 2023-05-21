using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ManaVampireScroll : SpellScroll
{
    [Constructible]
    public ManaVampireScroll(int amount = 1) : base(52, 0x1F61, amount)
    {
    }
}
