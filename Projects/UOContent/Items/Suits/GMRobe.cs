using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GMRobe : BaseSuit
{
    // [Constructible] // Deprecated, use StaffRobe
    public GMRobe() : base(AccessLevel.GameMaster, 0x26, 0x204F)
    {
    }
}
