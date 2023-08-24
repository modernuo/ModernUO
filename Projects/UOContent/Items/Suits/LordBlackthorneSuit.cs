using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LordBlackthorneSuit : BaseSuit
{
    [Constructible]
    public LordBlackthorneSuit() : base(AccessLevel.GameMaster, 0x0, 0x2043)
    {
    }
}
