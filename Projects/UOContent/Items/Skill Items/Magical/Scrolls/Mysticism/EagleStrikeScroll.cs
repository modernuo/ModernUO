using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EagleStrikeScroll : SpellScroll
{
    [Constructible]
    public EagleStrikeScroll(int amount = 1) : base(682, 0x2DA3, amount)
    {
    }
}
