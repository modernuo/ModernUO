using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CounselorRobe : BaseSuit
{
    // [Constructible] // Deprecated, use StaffRobe
    public CounselorRobe() : base(AccessLevel.Counselor, 0x3, 0x204F)
    {
    }
}
