using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SeerRobe : BaseSuit
{
    // [Constructible] // Deprecated, use StaffRobe
    public SeerRobe() : base(AccessLevel.Seer, 0x1D3, 0x204F)
    {
    }
}
