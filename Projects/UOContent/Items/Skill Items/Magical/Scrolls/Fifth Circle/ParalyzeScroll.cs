using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ParalyzeScroll : SpellScroll
{
    [Constructible]
    public ParalyzeScroll(int amount = 1) : base(37, 0x1F52, amount)
    {
    }
}
