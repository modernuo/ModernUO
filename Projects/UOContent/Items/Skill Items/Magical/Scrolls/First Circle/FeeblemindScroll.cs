using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FeeblemindScroll : SpellScroll
{
    [Constructible]
    public FeeblemindScroll(int amount = 1) : base(2, 0x1F30, amount)
    {
    }
}
