using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EnchantScroll : SpellScroll
{
    [Constructible]
    public EnchantScroll(int amount = 1) : base(680, 0x2DA1, amount)
    {
    }
}
