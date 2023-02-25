using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CurseScroll : SpellScroll
{
    [Constructible]
    public CurseScroll(int amount = 1) : base(26, 0x1F47, amount)
    {
    }
}
