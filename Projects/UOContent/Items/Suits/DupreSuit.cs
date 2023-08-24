using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DupreSuit : BaseSuit
{
    [Constructible]
    public DupreSuit() : base(AccessLevel.GameMaster, 0x0, 0x2050)
    {
    }
}
