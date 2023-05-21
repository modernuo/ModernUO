using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DispelScroll : SpellScroll
{
    [Constructible]
    public DispelScroll(int amount = 1) : base(40, 0x1F55, amount)
    {
    }
}
