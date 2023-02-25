using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ReactiveArmorScroll : SpellScroll
{
    [Constructible]
    public ReactiveArmorScroll(int amount = 1) : base(6, 0x1F2D, amount)
    {
    }
}
