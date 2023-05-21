using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PurgeMagicScroll : SpellScroll
{
    [Constructible]
    public PurgeMagicScroll(int amount = 1) : base(679, 0x2DA0, amount)
    {
    }
}
