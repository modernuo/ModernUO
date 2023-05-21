using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MassCurseScroll : SpellScroll
{
    [Constructible]
    public MassCurseScroll(int amount = 1) : base(45, 0x1F5A, amount)
    {
    }
}
