using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ExorcismScroll : SpellScroll
{
    [Constructible]
    public ExorcismScroll(int amount = 1) : base(116, 0x2270, amount)
    {
    }
}
