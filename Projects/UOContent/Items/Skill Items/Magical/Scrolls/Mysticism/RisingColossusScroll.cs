using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RisingColossusScroll : SpellScroll
{
    [Constructible]
    public RisingColossusScroll(int amount = 1) : base(692, 0x2DAD, amount)
    {
    }
}
