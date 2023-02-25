using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ParalyzeFieldScroll : SpellScroll
{
    [Constructible]
    public ParalyzeFieldScroll(int amount = 1) : base(46, 0x1F5B, amount)
    {
    }
}
