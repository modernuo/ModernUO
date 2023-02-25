using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class WraithFormScroll : SpellScroll
{
    [Constructible]
    public WraithFormScroll(int amount = 1) : base(115, 0x226F, amount)
    {
    }
}
