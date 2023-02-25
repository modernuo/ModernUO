using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TeleportScroll : SpellScroll
{
    [Constructible]
    public TeleportScroll(int amount = 1) : base(21, 0x1F42, amount)
    {
    }
}
