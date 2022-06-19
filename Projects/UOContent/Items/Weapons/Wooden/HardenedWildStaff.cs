using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class HardenedWildStaff : WildStaff
    {
        [Constructible]
        public HardenedWildStaff() => Attributes.WeaponDamage = 5;

        public override int LabelNumber => 1073552; // hardened wild staff
    }
}
