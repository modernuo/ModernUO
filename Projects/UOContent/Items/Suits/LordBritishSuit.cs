using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LordBritishSuit : BaseSuit
{
    [Constructible]
    public LordBritishSuit() : base(AccessLevel.GameMaster, 0x0, 0x2042)
    {
    }
}
