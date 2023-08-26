using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial  class AdminRobe : BaseSuit
{
    // [Constructible] // Deprecated, use StaffRobe
    public AdminRobe() : base(AccessLevel.Administrator, 0x0, 0x204F) // Blank hue
    {
    }
}
