using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ArchProtectionScroll : SpellScroll
{
    [Constructible]
    public ArchProtectionScroll(int amount = 1) : base(25, 0x1F46, amount)
    {
    }
}
