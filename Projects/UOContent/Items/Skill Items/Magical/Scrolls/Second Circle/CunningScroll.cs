using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CunningScroll : SpellScroll
{
    [Constructible]
    public CunningScroll(int amount = 1) : base(9, 0x1F36, amount)
    {
    }
}
