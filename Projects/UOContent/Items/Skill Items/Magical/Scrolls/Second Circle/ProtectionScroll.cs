using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ProtectionScroll : SpellScroll
{
    [Constructible]
    public ProtectionScroll(int amount = 1) : base(14, 0x1F3B, amount)
    {
    }
}
