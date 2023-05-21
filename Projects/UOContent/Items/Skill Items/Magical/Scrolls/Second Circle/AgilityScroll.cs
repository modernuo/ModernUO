using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AgilityScroll : SpellScroll
{
    [Constructible]
    public AgilityScroll(int amount = 1) : base(8, 0x1F35, amount)
    {
    }
}
