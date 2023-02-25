using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FireFieldScroll : SpellScroll
{
    [Constructible]
    public FireFieldScroll(int amount = 1) : base(27, 0x1F48, amount)
    {
    }
}
