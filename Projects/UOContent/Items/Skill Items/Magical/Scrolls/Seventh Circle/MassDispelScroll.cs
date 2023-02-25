using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MassDispelScroll : SpellScroll
{
    [Constructible]
    public MassDispelScroll(int amount = 1) : base(53, 0x1F62, amount)
    {
    }
}
