using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ArcanistsWildStaff : WildStaff
    {
        [Constructible]
        public ArcanistsWildStaff()
        {
            Attributes.BonusMana = 3;
            Attributes.WeaponDamage = 3;
        }

        public override int LabelNumber => 1073549; // arcanist's wild staff
    }
}
