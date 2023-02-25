using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class StrangleScroll : SpellScroll
{
    [Constructible]
    public StrangleScroll(int amount = 1) : base(110, 0x226A, amount)
    {
    }
}
