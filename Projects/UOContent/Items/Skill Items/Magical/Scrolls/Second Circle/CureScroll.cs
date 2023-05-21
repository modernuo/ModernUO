using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CureScroll : SpellScroll
{
    [Constructible]
    public CureScroll(int amount = 1) : base(10, 0x1F37, amount)
    {
    }
}
