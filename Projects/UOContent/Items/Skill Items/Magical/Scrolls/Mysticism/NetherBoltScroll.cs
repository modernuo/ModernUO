using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class NetherBoltScroll : SpellScroll
{
    [Constructible]
    public NetherBoltScroll(int amount = 1) : base(677, 0x2D9E, amount)
    {
    }
}
