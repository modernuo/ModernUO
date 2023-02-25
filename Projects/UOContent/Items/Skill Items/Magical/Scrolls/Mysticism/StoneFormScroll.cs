using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class StoneFormScroll : SpellScroll
{
    [Constructible]
    public StoneFormScroll(int amount = 1) : base(684, 0x2DA5, amount)
    {
    }
}
